using Microsoft.EntityFrameworkCore;
using SISReservas.Api.DTOs.Permisos;
using SISReservas.Api.DTOs.Reservas;
using SISReservas.Api.Models;
using SISReservas.Api.Models.Enums;
using SISReservas.Api.Services;

namespace SISReservas.Api.Tests;

public class NotificacionesTests
{
    [Fact]
    public async Task GuardadoFallido_NoDejaSolicitudNiNotificacionesYNoSeFiltranEnReintento()
    {
        // Arrange
        using var datos = new TestDatabase();
        var fallo = new FailingSaveChangesInterceptor();
        var db = datos.CreateContext(fallo);
        await TestData.AgregarUsuarioAsync(db, Rol.Administrador);
        var usuario = await TestData.AgregarUsuarioAsync(db);
        var servicio = TestData.CrearSolicitudService(db);
        fallo.Fail = true;
        // Act
        var fallida = await servicio.CrearAsync(usuario.Id, new() { Permiso = Permiso.ReservarEspacios });
        fallo.Fail = false;
        await db.SaveChangesAsync();
        // Assert
        Assert.False(fallida.Exito);
        var recarga = datos.CreateContext();
        Assert.Empty(recarga.SolicitudesPermiso);
        Assert.Empty(recarga.Notificaciones);
        Assert.Empty(recarga.CorreosEnCola);
    }

    [Fact]
    public async Task PreferenciaNoDeshabilitaCorreosDeSeguridad_YUsuarioInactivoNoConsulta()
    {
        // Arrange
        using var datos = new TestDatabase();
        var db = datos.CreateContext();
        var usuario = await TestData.AgregarUsuarioAsync(db);
        var servicio = new NotificacionService(db);
        await servicio.PreferenciasAsync(usuario.Id, false);
        // Act
        await new EmailQueueService(db).QueueAsync(usuario.Id, usuario.Email, "Activación", "Seguridad");
        usuario.Activo = false;
        await db.SaveChangesAsync();
        // Assert
        Assert.Single(db.CorreosEnCola);
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => servicio.ConsultarAsync(usuario.Id));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => servicio.PreferenciasAsync(usuario.Id, true));
    }

    [Fact]
    public async Task CrearSolicitud_NotificaSoloAdministradoresActivos_YPersisteAmbosCanales()
    {
        // Arrange
        using var datos = new TestDatabase();
        var db = datos.CreateContext();
        var uno = await TestData.AgregarUsuarioAsync(db, Rol.Administrador);
        var dos = await TestData.AgregarUsuarioAsync(db, Rol.Administrador);
        await TestData.AgregarUsuarioAsync(db, Rol.Administrador, false);
        var usuario = await TestData.AgregarUsuarioAsync(db);
        // Act
        var resultado = await TestData.CrearSolicitudService(db).CrearAsync(usuario.Id, new() { Permiso = Permiso.ReservarEspacios });
        // Assert
        Assert.True(resultado.Exito);
        var recarga = datos.CreateContext();
        Assert.Equal(new[] { uno.Id, dos.Id }.Order(), (await recarga.Notificaciones.Select(n => n.UsuarioId).ToListAsync()).Order());
        Assert.Equal(2, await recarga.CorreosEnCola.CountAsync());
        Assert.All(await recarga.Notificaciones.ToListAsync(), n => Assert.Null(n.LeidaEn));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Resolver_NotificaResultadoYMotivo_UnaSolaVez(bool aprobar)
    {
        // Arrange
        using var datos = new TestDatabase();
        var db = datos.CreateContext();
        var admin = await TestData.AgregarUsuarioAsync(db, Rol.Administrador);
        var usuario = await TestData.AgregarUsuarioAsync(db);
        var servicio = TestData.CrearSolicitudService(db);
        var creada = await servicio.CrearAsync(usuario.Id, new() { Permiso = Permiso.ReservarEspacios });
        // Act
        var resultado = aprobar ? await servicio.AprobarAsync(creada.Valor!.Id, admin.Id)
            : await servicio.RechazarAsync(creada.Valor!.Id, admin.Id, new() { MotivoRechazo = "Motivo comprobable" });
        var duplicada = await servicio.AprobarAsync(creada.Valor.Id, admin.Id);
        // Assert
        Assert.True(resultado.Exito);
        Assert.False(duplicada.Exito);
        var notificacion = await db.Notificaciones.SingleAsync(n => n.UsuarioId == usuario.Id);
        Assert.Contains(aprobar ? "aprobada" : "Motivo comprobable", notificacion.Mensaje);
    }

    [Fact]
    public async Task Bandeja_LecturaYPreferencia_SoloPropiasPersistentes()
    {
        // Arrange
        using var datos = new TestDatabase();
        var db = datos.CreateContext();
        var usuario = await TestData.AgregarUsuarioAsync(db);
        var otro = await TestData.AgregarUsuarioAsync(db);
        var servicio = new NotificacionService(db);
        await servicio.PreferenciasAsync(usuario.Id, false);
        await servicio.PrepararAsync([usuario.Id], "Prueba", "Mensaje");
        await db.SaveChangesAsync();
        var id = (await db.Notificaciones.SingleAsync()).Id;
        // Act
        Assert.False(await servicio.LeerAsync(otro.Id, id));
        Assert.True(await servicio.LeerAsync(usuario.Id, id));
        var fecha = (await db.Notificaciones.SingleAsync()).LeidaEn;
        await servicio.LeerAsync(usuario.Id, id);
        var recarga = new NotificacionService(datos.CreateContext());
        // Assert
        Assert.Equal(fecha, (await db.Notificaciones.SingleAsync()).LeidaEn);
        Assert.Empty(db.CorreosEnCola);
        Assert.Empty((await recarga.ConsultarAsync(otro.Id)).Notificaciones);
        Assert.Equal(0, (await recarga.ConsultarAsync(usuario.Id)).NoLeidas);
        Assert.False((await recarga.PreferenciasAsync(usuario.Id)).CorreoHabilitado);
    }

    [Fact]
    public async Task ConfirmacionReserva_NotificaPropietario_SinEventoEnTransicionInvalida()
    {
        // Arrange
        using var datos = new TestDatabase();
        var db = datos.CreateContext();
        var admin = await TestData.AgregarUsuarioAsync(db, Rol.Administrador);
        var espacio = new Espacio { Nombre = "Cancha", Tipo = TipoEspacio.Cancha, Activo = true };
        db.Espacios.Add(espacio);
        await db.SaveChangesAsync();
        var servicio = new ReservaService(db, new PermisoService(db), TimeProvider.System);
        var creada = await servicio.CrearAsync(admin.Id, new() { EspacioId = espacio.Id, FechaInicio = DateTime.UtcNow.AddDays(1), FechaFin = DateTime.UtcNow.AddDays(1).AddHours(1) });
        // Act
        var confirma = await servicio.ConfirmarAsync(admin.Id, creada.Valor!.Id);
        var repite = await servicio.ConfirmarAsync(admin.Id, creada.Valor.Id);
        // Assert
        Assert.True(confirma.Exito);
        Assert.False(repite.Exito);
        Assert.Equal(admin.Id, (await db.Notificaciones.SingleAsync()).UsuarioId);
        Assert.Single(db.CorreosEnCola);
    }
}
