using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using SISReservas.Api.Controllers;
using SISReservas.Api.DTOs.Auth;
using SISReservas.Api.Models.Enums;
using SISReservas.Api.Security;
using SISReservas.Api.Services;

namespace SISReservas.Api.Tests;

public class RevisionAcumuladaTests
{
    private sealed class Escenario : IDisposable
    {
        public TestDatabase Database { get; } = new();
        public SISReservas.Api.Data.SISReservasDbContext Db { get; }
        public PasswordService Passwords { get; } = new();
        public SessionService Sesiones { get; }
        public AuthService Auth { get; }
        public PasswordRecoveryService Recuperacion { get; }
        public Escenario()
        {
            Db = Database.CreateContext();
            var tokens = new TokenService();
            var cola = new EmailQueueService(Db);
            Sesiones = new(Db, tokens);
            Auth = new(Db, Passwords, tokens, cola, new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?> { ["AppBaseUrl"] = "http://localhost:5054" }).Build(), Sesiones);
            Recuperacion = new(Db, Passwords, cola, Sesiones, new PasswordPolicyService());
        }
        public void Dispose() => Database.Dispose();
    }

    [Fact]
    public async Task Registro_EncolaActivacionYTokenActivaUnaSolaVez_SinEnviarCorreo()
    {
        // Arrange
        using var e = new Escenario();
        var request = new RegisterRequest { Nombre = "Prueba", Email = "activacion@test.invalid", Password = "Prueba1234" };
        // Act
        await e.Auth.RegisterAsync(request);
        // Assert: solo se comprueba la cola, no entrega SMTP.
        var usuario = await e.Db.Usuarios.SingleAsync();
        Assert.False(usuario.Activo);
        var correo = await e.Db.CorreosEnCola.SingleAsync();
        Assert.Equal(EstadoCorreo.Pendiente, correo.Estado);
        var enlace = Regex.Match(correo.Cuerpo, @"https?://\S+").Value;
        var token = Uri.UnescapeDataString(enlace.Split("&token=")[1]);

        // Act
        await e.Auth.ActivateAsync(request.Email, token);
        // Assert
        Assert.True(usuario.Activo);
        Assert.True((await e.Db.TokensAcceso.SingleAsync()).Usado);
        await Assert.ThrowsAsync<InvalidOperationException>(() => e.Auth.ActivateAsync(request.Email, token));
    }

    [Fact]
    public async Task LoginYLogout_SesionValidaAntesYRevocadaDespues()
    {
        // Arrange
        using var e = new Escenario();
        var usuario = await TestData.AgregarUsuarioAsync(e.Db);
        usuario.PasswordHash = e.Passwords.HashPassword(usuario, "Prueba1234");
        await e.Db.SaveChangesAsync();
        // Act
        var login = await e.Auth.LoginAsync(usuario.Email, "Prueba1234");
        var valida = await e.Sesiones.GetValidSessionAsync(login.SessionToken!);
        await e.Sesiones.RevokeAsync(login.SessionToken!);
        var revocada = await e.Sesiones.GetValidSessionAsync(login.SessionToken!);
        // Assert
        Assert.False(string.IsNullOrWhiteSpace(login.SessionToken));
        Assert.NotNull(valida);
        Assert.Null(revocada);
    }

    [Fact]
    public async Task Recuperacion_CambiaPasswordRevocaSesionYRechazaReutilizacion_SinEnviarCorreo()
    {
        // Arrange
        using var e = new Escenario();
        var usuario = await TestData.AgregarUsuarioAsync(e.Db);
        usuario.PasswordHash = e.Passwords.HashPassword(usuario, "Anterior123");
        await e.Db.SaveChangesAsync();
        var sesion = await e.Sesiones.CreateAsync(usuario);
        // Act
        await e.Recuperacion.RequestRecoveryAsync(usuario.Email);
        var correo = await e.Db.CorreosEnCola.SingleAsync();
        var token = Regex.Match(correo.Cuerpo, @"Código de recuperación: (\S+)").Groups[1].Value;
        var cambio = await e.Recuperacion.ResetPasswordAsync(token, "NuevaClave456");
        // Assert
        Assert.Equal(EstadoCorreo.Pendiente, correo.Estado);
        Assert.True(cambio);
        Assert.True(e.Passwords.VerifyPassword(usuario, usuario.PasswordHash, "NuevaClave456"));
        Assert.False(e.Passwords.VerifyPassword(usuario, usuario.PasswordHash, "Anterior123"));
        Assert.Null(await e.Sesiones.GetValidSessionAsync(sesion.RawToken));
        Assert.False(await e.Recuperacion.ResetPasswordAsync(token, "OtraClave789"));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Cola_EntregaActivacionORecuperacionPorSmtp(bool recuperacion)
    {
        using var e = new Escenario();
        const string correo = "entrega@test.invalid";
        await e.Auth.RegisterAsync(new RegisterRequest
        {
            Nombre = "Prueba SMTP", Email = correo, Password = "Prueba1234"
        });
        if (recuperacion)
        {
            // Aislar el mensaje de recuperación en la base InMemory de esta prueba.
            var activacion = await e.Db.CorreosEnCola.SingleAsync();
            e.Db.CorreosEnCola.Remove(activacion);
            await e.Db.SaveChangesAsync();
            await e.Recuperacion.RequestRecoveryAsync("  " + correo.ToUpperInvariant() + "  ");
        }
        var mensaje = await e.Db.CorreosEnCola.SingleAsync();
        Assert.Equal(EstadoCorreo.Pendiente, mensaje.Estado);

        using var tiempo = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        using var servidor = new TcpListener(IPAddress.Loopback, 0);
        servidor.Start();
        var puerto = ((IPEndPoint)servidor.LocalEndpoint).Port;
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["SMTP_HOST"] = "127.0.0.1",
            ["SMTP_PORT"] = puerto.ToString(System.Globalization.CultureInfo.InvariantCulture),
            ["SMTP_FROM"] = "reservas@test.invalid",
            ["SMTP_ENABLE_SSL"] = "false",
            ["SMTP_USE_AUTH"] = "false"
        }).Build();
        var remitente = new EmailSenderService(e.Db, config, NullLogger<EmailSenderService>.Instance);

        var recepcion = RecibirSmtpAsync(servidor, tiempo.Token);
        await Task.WhenAll(remitente.SendPendingAsync(tiempo.Token), recepcion);

        var captura = await recepcion;
        Assert.True(captura.Contains("RCPT TO:<entrega@test.invalid>", StringComparison.OrdinalIgnoreCase));
        var separador = captura.IndexOf("\n\n", StringComparison.Ordinal);
        Assert.True(separador >= 0);
        var cuerpo = captura[(separador + 2)..];
        if (captura[..separador].Contains("Content-Transfer-Encoding: base64", StringComparison.OrdinalIgnoreCase))
            cuerpo = Encoding.UTF8.GetString(Convert.FromBase64String(cuerpo));
        Assert.True(cuerpo.Contains(recuperacion ? "expira en 30 minutos" : "/api/auth/activate",
            StringComparison.OrdinalIgnoreCase));
        var procesado = await e.Database.CreateContext().CorreosEnCola.SingleAsync();
        Assert.Equal(EstadoCorreo.Enviado, procesado.Estado);
        Assert.NotNull(procesado.EnviadoEn);
    }

    private static async Task<string> RecibirSmtpAsync(TcpListener servidor, CancellationToken ct)
    {
        using var cliente = await servidor.AcceptTcpClientAsync(ct);
        using var stream = cliente.GetStream();
        using var lector = new StreamReader(stream);
        using var escritor = new StreamWriter(stream, Encoding.ASCII) { NewLine = "\r\n", AutoFlush = true };
        var captura = new StringBuilder();
        await escritor.WriteLineAsync("220 SMTP de prueba");

        while (await lector.ReadLineAsync(ct) is { } linea)
        {
            captura.Append(linea).Append('\n');
            if (!linea.Equals("DATA", StringComparison.OrdinalIgnoreCase))
            {
                await escritor.WriteLineAsync("250 OK");
                continue;
            }

            await escritor.WriteLineAsync("354 Enviar mensaje");
            while (await lector.ReadLineAsync(ct) is { } dato)
            {
                if (dato == ".")
                {
                    await escritor.WriteLineAsync("250 Mensaje recibido");
                    return captura.ToString();
                }
                captura.Append(dato).Append('\n');
            }
        }

        throw new IOException("El cliente SMTP cerró la conexión antes de completar el mensaje.");
    }

    [Theory]
    [InlineData(typeof(PermissionRequestsController), nameof(PermissionRequestsController.GetPending))]
    [InlineData(typeof(PermissionRequestsController), nameof(PermissionRequestsController.Approve))]
    [InlineData(typeof(UsersController), nameof(UsersController.ChangeRole))]
    public void OperacionAdministrativa_DeclaraRestriccionAdministrador(Type controlador, string operacion)
    {
        // Arrange
        var metodo = controlador.GetMethod(operacion)!;
        // Act
        var restriccion = metodo.GetCustomAttribute<AuthorizeAttribute>();
        // Assert: verificación de metadatos, no una solicitud HTTP real.
        Assert.Equal(nameof(Rol.Administrador), restriccion!.Roles);
    }
}
