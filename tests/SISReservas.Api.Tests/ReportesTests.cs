using SISReservas.Api.Models;
using SISReservas.Api.Models.Enums;
using SISReservas.Api.Services;

namespace SISReservas.Api.Tests;

public class ReportesTests
{
    [Fact]
    public async Task Solicitudes_AgrupaEnRango_YAislaPropietarios()
    {
        // Arrange
        using var datos = new TestDatabase();
        var db = datos.CreateContext();
        var admin = await TestData.AgregarUsuarioAsync(db, Rol.Administrador);
        var uno = await TestData.AgregarUsuarioAsync(db);
        var dos = await TestData.AgregarUsuarioAsync(db);
        var inicio = new DateTime(2026, 11, 1, 0, 0, 0, DateTimeKind.Utc);
        await TestData.AgregarSolicitudAsync(db, uno, creadaEn: inicio);
        await TestData.AgregarSolicitudAsync(db, uno, creadaEn: inicio.AddHours(1));
        await TestData.AgregarSolicitudAsync(db, dos, creadaEn: inicio);
        await TestData.AgregarSolicitudAsync(db, uno, estado: EstadoSolicitudPermiso.Rechazada, creadaEn: inicio.AddDays(2));
        var servicio = new ReporteService(db);
        // Act
        var propio = await servicio.SolicitudesAsync(uno.Id, inicio, inicio.AddDays(1));
        var global = await servicio.SolicitudesAsync(admin.Id, inicio, inicio.AddDays(1));
        var otroRango = await servicio.SolicitudesAsync(uno.Id, inicio.AddDays(2), inicio.AddDays(3));
        var vacio = await servicio.SolicitudesAsync(uno.Id, inicio.AddYears(1), inicio.AddYears(1).AddDays(1));
        // Assert
        Assert.Equal(2, propio.Total);
        Assert.Single(propio.Grupos);
        Assert.Equal(3, global.Total);
        Assert.Equal(1, otroRango.Total);
        Assert.Equal(EstadoSolicitudPermiso.Rechazada, otroRango.Grupos.Single().Estado);
        Assert.Equal(0, vacio.Total);
        Assert.Empty(vacio.Grupos);
    }

    [Fact]
    public async Task Reservas_SumaHorasDeInterseccion_ExcluyeCanceladasYAjenas()
    {
        // Arrange
        using var datos = new TestDatabase();
        var db = datos.CreateContext();
        var admin = await TestData.AgregarUsuarioAsync(db, Rol.Administrador);
        var uno = await TestData.AgregarUsuarioAsync(db);
        var dos = await TestData.AgregarUsuarioAsync(db);
        var espacio = new Espacio { Nombre = "Cancha", Activo = true, Tipo = TipoEspacio.Cancha };
        var inicio = new DateTime(2026, 11, 1, 0, 0, 0, DateTimeKind.Utc);
        foreach (var usuario in new[] { uno, uno, dos })
            db.Reservas.Add(new Reserva { UsuarioId = usuario.Id, Espacio = espacio, FechaInicio = inicio.AddHours(-1), FechaFin = inicio.AddHours(2) });
        db.Reservas.Add(new Reserva { UsuarioId = uno.Id, Espacio = espacio, FechaInicio = inicio, FechaFin = inicio.AddHours(1), Estado = EstadoReserva.Cancelada });
        await db.SaveChangesAsync();
        var servicio = new ReporteService(db);
        // Act
        var propio = await servicio.ReservasAsync(uno.Id, inicio, inicio.AddHours(1));
        var global = await servicio.ReservasAsync(admin.Id, inicio, inicio.AddHours(1));
        var vacio = await servicio.ReservasAsync(uno.Id, inicio.AddDays(1), inicio.AddDays(2));
        // Assert
        Assert.Equal(2, propio.TotalReservas);
        Assert.Equal(2, propio.TotalHoras);
        Assert.Single(propio.Grupos);
        Assert.Equal(3, global.TotalHoras);
        Assert.Equal(0, vacio.TotalHoras);
        Assert.Empty(vacio.Grupos);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task FechasInvalidasYUsuarioInactivo_Rechazados(bool reservas)
    {
        // Arrange
        using var datos = new TestDatabase();
        var db = datos.CreateContext();
        var usuario = await TestData.AgregarUsuarioAsync(db);
        var servicio = new ReporteService(db);
        var inicio = DateTime.UtcNow;
        // Act / Assert
        if (reservas)
            await Assert.ThrowsAsync<ArgumentException>(() => servicio.ReservasAsync(usuario.Id, inicio, inicio));
        else
            await Assert.ThrowsAsync<ArgumentException>(() => servicio.SolicitudesAsync(usuario.Id, DateTime.SpecifyKind(inicio, DateTimeKind.Unspecified), inicio.AddHours(1)));
        usuario.Activo = false;
        await db.SaveChangesAsync();
        if (reservas)
            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => servicio.ReservasAsync(usuario.Id, inicio, inicio.AddDays(1)));
        else
            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => servicio.SolicitudesAsync(usuario.Id, inicio, inicio.AddDays(1)));
    }
}
