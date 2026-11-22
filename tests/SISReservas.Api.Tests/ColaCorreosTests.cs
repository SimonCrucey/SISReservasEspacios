using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using SISReservas.Api.Models.Enums;
using SISReservas.Api.Services;

namespace SISReservas.Api.Tests;

public class ColaCorreosTests
{
    [Fact]
    public async Task FalloConfiguracion_TresIntentosYEstadoFallido_NoReintentaIndefinidamente()
    {
        // Arrange
        using var datos = new TestDatabase();
        var db = datos.CreateContext();
        var usuario = await TestData.AgregarUsuarioAsync(db);
        await new EmailQueueService(db).QueueAsync(usuario.Id, usuario.Email, "Prueba", "Mensaje");
        var servicio = new EmailSenderService(db, new ConfigurationBuilder().Build(), NullLogger<EmailSenderService>.Instance);
        // Act
        for (var i = 0; i < 4; i++) await servicio.SendPendingAsync();
        // Assert
        var correo = await datos.CreateContext().CorreosEnCola.SingleAsync();
        Assert.Equal(3, correo.Intentos);
        Assert.Equal(EstadoCorreo.Fallido, correo.Estado);
        Assert.NotNull(correo.UltimoIntentoEn);
        Assert.NotEmpty(correo.UltimoError!);
        Assert.Null(correo.EnviadoEn);
        Assert.Null(correo.ReclamoId);
    }

    [Fact]
    public async Task ReclamoInterrumpido_RecuperaVencido_PeroRespetaReclamoVigente()
    {
        // Arrange
        using var datos = new TestDatabase();
        var db = datos.CreateContext();
        var usuario = await TestData.AgregarUsuarioAsync(db);
        var cola = new EmailQueueService(db);
        await cola.QueueAsync(usuario.Id, usuario.Email, "Vencido", "Mensaje");
        await cola.QueueAsync(usuario.Id, usuario.Email, "Vigente", "Mensaje");
        var vencido = await db.CorreosEnCola.SingleAsync(c => c.Asunto == "Vencido");
        var vigente = await db.CorreosEnCola.SingleAsync(c => c.Asunto == "Vigente");
        vencido.ReclamoHasta = DateTime.UtcNow.AddMinutes(-1);
        vencido.Intentos = 3;
        vigente.ReclamoHasta = DateTime.UtcNow.AddMinutes(1);
        vigente.Intentos = 1;
        await db.SaveChangesAsync();
        var servicio = new EmailSenderService(db, new ConfigurationBuilder().Build(), NullLogger<EmailSenderService>.Instance);
        // Act
        await servicio.SendPendingAsync();
        // Assert
        Assert.Equal(EstadoCorreo.Fallido, vencido.Estado);
        Assert.Equal(EstadoCorreo.Pendiente, vigente.Estado);
        Assert.Equal(1, vigente.Intentos);
    }

    [Fact]
    public async Task ProcesadoresConcurrentes_RespetanLimiteDeIntentos()
    {
        // Arrange
        using var datos = new TestDatabase();
        var db = datos.CreateContext();
        var usuario = await TestData.AgregarUsuarioAsync(db);
        await new EmailQueueService(db).QueueAsync(usuario.Id, usuario.Email, "Prueba", "Mensaje");
        var config = new ConfigurationBuilder().Build();
        var uno = new EmailSenderService(datos.CreateContext(), config, NullLogger<EmailSenderService>.Instance);
        var dos = new EmailSenderService(datos.CreateContext(), config, NullLogger<EmailSenderService>.Instance);
        // Act
        await Task.WhenAll(uno.SendPendingAsync(), dos.SendPendingAsync());
        // Assert
        var correo = await datos.CreateContext().CorreosEnCola.SingleAsync();
        Assert.Equal(2, correo.Intentos);
        Assert.Equal(EstadoCorreo.Pendiente, correo.Estado);
    }
}
