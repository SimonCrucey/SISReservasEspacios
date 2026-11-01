using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SISReservas.Api.Controllers;
using SISReservas.Api.DTOs.Reservas;
using SISReservas.Api.DTOs.Permisos;
using SISReservas.Api.Models;
using SISReservas.Api.Models.Enums;
using SISReservas.Api.Services;

namespace SISReservas.Api.Tests;

public class ReservasAutorizacionTests
{
    [Fact]
    public void Endpoints_ExigenEsquemaSessionYRolesExistentes()
    {
        // Arrange
        var tipo = typeof(ReservasController);
        // Act
        var autorizacion = tipo.GetCustomAttribute<AuthorizeAttribute>();
        // Assert
        Assert.NotNull(autorizacion);
        Assert.Equal("Session", autorizacion.AuthenticationSchemes);
        Assert.Equal("Estandar,Administrador", autorizacion.Roles);
        Assert.DoesNotContain(tipo.GetMethods(), m => m.GetCustomAttributes<AllowAnonymousAttribute>().Any());
    }

    [Fact]
    public void Confirmar_ExigeAdministradorYNoExisteEndpointCompletar()
    {
        // Arrange
        var tipo = typeof(ReservasController);
        // Act
        var autorizacion = tipo.GetMethod(nameof(ReservasController.Confirmar))!.GetCustomAttribute<AuthorizeAttribute>();
        // Assert
        Assert.Equal(nameof(Rol.Administrador), autorizacion!.Roles);
        Assert.DoesNotContain(tipo.GetMethods(), m => m.Name.Contains("Completar"));
    }

    [Fact]
    public async Task Endpoint_SinIdentificadorDeSesion_Rechaza()
    {
        // Arrange
        using var database = new TestDatabase(); var db = database.CreateContext();
        var controller = new ReservasController(new ReservaService(db, new PermisoService(db), TimeProvider.System))
        {
            ControllerContext = new ControllerContext { HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext() }
        };
        // Act
        var resultado = await controller.ListarEspacios();
        // Assert
        Assert.IsType<UnauthorizedResult>(resultado);
    }

    [Fact]
    public async Task Crear_SolicitudPendienteRechazaYTrasAprobacionRealPermite()
    {
        // Arrange
        using var database = new TestDatabase(); var db = database.CreateContext();
        var usuario = await TestData.AgregarUsuarioAsync(db);
        var admin = await TestData.AgregarUsuarioAsync(db, Rol.Administrador);
        var espacio = new Espacio { Nombre = "Cancha", Tipo = TipoEspacio.Cancha };
        db.Espacios.Add(espacio); await db.SaveChangesAsync();
        var permisos = new PermisoService(db);
        var solicitudes = new SolicitudPermisoService(db, permisos);
        var reservas = new ReservaService(db, permisos, TimeProvider.System);
        var request = new CrearReservaDto { EspacioId = espacio.Id, FechaInicio = DateTime.UtcNow.AddDays(1), FechaFin = DateTime.UtcNow.AddDays(1).AddHours(1) };

        // Act: tampoco puede reservar antes de crear una solicitud.
        var sinSolicitud = await reservas.CrearAsync(usuario.Id, request);
        // Assert
        Assert.Equal(MotivoResultadoReserva.Prohibido, sinSolicitud.Motivo);
        Assert.Empty(db.Reservas);

        // Act: crea la solicitud usando Gestión de permisos real.
        var solicitud = await solicitudes.CrearAsync(usuario.Id, new CrearSolicitudPermisoDto { Permiso = Permiso.ReservarEspacios });
        // Assert
        Assert.True(solicitud.Exito);
        Assert.Equal(EstadoSolicitudPermiso.Pendiente, solicitud.Valor!.Estado);
        Assert.False(await permisos.TienePermisoAsync(usuario.Id, Permiso.ReservarEspacios));

        // Act: sin aprobación no puede reservar.
        var antes = await reservas.CrearAsync(usuario.Id, request);
        // Assert
        Assert.Equal(MotivoResultadoReserva.Prohibido, antes.Motivo);
        Assert.Empty(db.Reservas);

        // Act: los componentes reales aprueban el permiso y ejecutan el negocio.
        var aprobacion = await solicitudes.AprobarAsync(solicitud.Valor!.Id, admin.Id);
        var despues = await reservas.CrearAsync(usuario.Id, request);
        // Assert
        Assert.True(aprobacion.Exito);
        Assert.True(await permisos.TienePermisoAsync(usuario.Id, Permiso.ReservarEspacios));
        Assert.True(despues.Exito);
        var creada = Assert.Single(db.Reservas);
        Assert.Equal(usuario.Id, creada.UsuarioId);
        Assert.Equal(EstadoReserva.Pendiente, creada.Estado);
    }
}
