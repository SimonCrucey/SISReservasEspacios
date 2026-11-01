using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
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
