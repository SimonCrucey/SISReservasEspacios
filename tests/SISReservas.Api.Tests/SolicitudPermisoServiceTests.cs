using Microsoft.EntityFrameworkCore;
using SISReservas.Api.DTOs.Permisos;
using SISReservas.Api.Models.Enums;
using SISReservas.Api.Services;

namespace SISReservas.Api.Tests;

public class SolicitudPermisoServiceTests
{
    [Fact]
    public async Task CrearAsync_SolicitudValida_NacePendiente()
    {
        using var database = new TestDatabase();
        using var db = database.CreateContext();

        var solicitante = await TestData.AgregarUsuarioAsync(db);

        var service = TestData.CrearSolicitudService(db);

        var resultado = await service.CrearAsync(
            solicitante.Id,
            new CrearSolicitudPermisoDto
            {
                Permiso = Permiso.ConsultarUsuarios
            });

        Assert.True(resultado.Exito);
        Assert.NotNull(resultado.Valor);
        Assert.Equal(
            EstadoSolicitudPermiso.Pendiente,
            resultado.Valor!.Estado);
        Assert.Null(resultado.Valor.ResueltaPorId);
        Assert.Null(resultado.Valor.ResueltaEn);
        Assert.Null(resultado.Valor.AplicadaEn);
        Assert.Null(resultado.Valor.MotivoRechazo);

        var persistida = await db.SolicitudesPermiso
            .AsNoTracking()
            .SingleAsync();

        Assert.Equal(
            EstadoSolicitudPermiso.Pendiente,
            persistida.Estado);
        Assert.Equal(solicitante.Id, persistida.SolicitanteId);
    }

    [Fact]
    public async Task ObtenerPendientesAsync_Administrador_VeTodasLasPendientes()
    {
        using var database = new TestDatabase();
        using var db = database.CreateContext();

        var administrador = await TestData.AgregarUsuarioAsync(
            db,
            Rol.Administrador,
            nombre: "Administrador");

        var usuarioUno = await TestData.AgregarUsuarioAsync(
            db,
            nombre: "Usuario Uno");

        var usuarioDos = await TestData.AgregarUsuarioAsync(
            db,
            nombre: "Usuario Dos");

        var ahora = DateTime.UtcNow;

        var pendienteUno = await TestData.AgregarSolicitudAsync(
            db,
            usuarioUno,
            creadaEn: ahora.AddMinutes(-2));

        var pendienteDos = await TestData.AgregarSolicitudAsync(
            db,
            usuarioDos,
            creadaEn: ahora.AddMinutes(-1));

        await TestData.AgregarSolicitudAsync(
            db,
            usuarioUno,
            estado: EstadoSolicitudPermiso.Aplicada);

        await TestData.AgregarSolicitudAsync(
            db,
            usuarioDos,
            estado: EstadoSolicitudPermiso.Rechazada);

        var service = TestData.CrearSolicitudService(db);

        var resultado = await service.ObtenerPendientesAsync(
            administrador.Id);

        Assert.True(resultado.Exito);
        Assert.NotNull(resultado.Valor);
        Assert.Equal(2, resultado.Valor!.Count);
        Assert.Equal(
            [pendienteUno.Id, pendienteDos.Id],
            resultado.Valor.Select(s => s.Id));
        Assert.All(
            resultado.Valor,
            s => Assert.Equal(
                EstadoSolicitudPermiso.Pendiente,
                s.Estado));
        Assert.Contains(
            resultado.Valor,
            s => s.SolicitanteNombre == usuarioUno.Nombre);
    }

    [Fact]
    public async Task ObtenerMiasAsync_Estandar_SoloDevuelveSusSolicitudes()
    {
        using var database = new TestDatabase();
        using var db = database.CreateContext();

        var usuarioUno = await TestData.AgregarUsuarioAsync(
            db,
            nombre: "Usuario Uno");

        var usuarioDos = await TestData.AgregarUsuarioAsync(
            db,
            nombre: "Usuario Dos");

        var propiaPendiente = await TestData.AgregarSolicitudAsync(
            db,
            usuarioUno);

        var propiaAplicada = await TestData.AgregarSolicitudAsync(
            db,
            usuarioUno,
            estado: EstadoSolicitudPermiso.Aplicada);

        var ajena = await TestData.AgregarSolicitudAsync(
            db,
            usuarioDos);

        var service = TestData.CrearSolicitudService(db);

        var resultado = await service.ObtenerMiasAsync(
            usuarioUno.Id);

        Assert.True(resultado.Exito);
        Assert.NotNull(resultado.Valor);
        Assert.Equal(2, resultado.Valor!.Count);
        Assert.All(
            resultado.Valor,
            s => Assert.Equal(usuarioUno.Id, s.SolicitanteId));
        Assert.Contains(
            resultado.Valor,
            s => s.Id == propiaPendiente.Id);
        Assert.Contains(
            resultado.Valor,
            s => s.Id == propiaAplicada.Id);
        Assert.DoesNotContain(
            resultado.Valor,
            s => s.Id == ajena.Id);
    }

    [Fact]
    public async Task ObtenerPendientesAsync_Estandar_DevuelveRolNoPermitido()
    {
        using var database = new TestDatabase();
        using var db = database.CreateContext();

        var estandar = await TestData.AgregarUsuarioAsync(db);

        var service = TestData.CrearSolicitudService(db);

        var resultado = await service.ObtenerPendientesAsync(
            estandar.Id);

        Assert.False(resultado.Exito);
        Assert.Equal(
            MotivoResultadoSolicitudPermiso.RolNoPermitido,
            resultado.Motivo);
        Assert.Null(resultado.Valor);
    }

    [Fact]
    public async Task AprobarAsync_Pendiente_RegistraResolutorYFecha()
    {
        using var database = new TestDatabase();
        using var db = database.CreateContext();

        var administrador = await TestData.AgregarUsuarioAsync(
            db,
            Rol.Administrador,
            nombre: "Administrador");

        var solicitante = await TestData.AgregarUsuarioAsync(db);

        var solicitud = await TestData.AgregarSolicitudAsync(
            db,
            solicitante);

        var antes = DateTime.UtcNow;

        var service = TestData.CrearSolicitudService(db);

        var resultado = await service.AprobarAsync(
            solicitud.Id,
            administrador.Id);

        var despues = DateTime.UtcNow;

        Assert.True(resultado.Exito);
        Assert.NotNull(resultado.Valor);
        Assert.Equal(
            EstadoSolicitudPermiso.Aplicada,
            resultado.Valor!.Estado);
        Assert.Equal(
            administrador.Id,
            resultado.Valor.ResueltaPorId);
        Assert.Equal(
            administrador.Nombre,
            resultado.Valor.ResueltaPorNombre);
        Assert.NotNull(resultado.Valor.ResueltaEn);
        Assert.InRange(
            resultado.Valor.ResueltaEn!.Value,
            antes,
            despues);
        Assert.NotNull(resultado.Valor.AplicadaEn);
        Assert.InRange(
            resultado.Valor.AplicadaEn!.Value,
            antes,
            despues);

        var persistida = await db.SolicitudesPermiso
            .AsNoTracking()
            .SingleAsync(s => s.Id == solicitud.Id);

        Assert.Equal(
            EstadoSolicitudPermiso.Aplicada,
            persistida.Estado);
        Assert.Equal(
            administrador.Id,
            persistida.ResueltaPorId);
        Assert.NotNull(persistida.ResueltaEn);
        Assert.NotNull(persistida.AplicadaEn);
    }

    [Fact]
    public async Task AprobarAsync_ConcedePermisoEfectivo()
    {
        using var database = new TestDatabase();
        using var db = database.CreateContext();

        var administrador = await TestData.AgregarUsuarioAsync(
            db,
            Rol.Administrador);

        var solicitante = await TestData.AgregarUsuarioAsync(db);

        var solicitud = await TestData.AgregarSolicitudAsync(
            db,
            solicitante);

        var service = TestData.CrearSolicitudService(db);

        var resultado = await service.AprobarAsync(
            solicitud.Id,
            administrador.Id);

        Assert.True(resultado.Exito);

        var permisoService = new PermisoService(db);

        Assert.True(
            await permisoService.TienePermisoAsync(
                solicitante.Id,
                Permiso.ConsultarUsuarios));

        var efectivos = await permisoService.ObtenerEfectivosAsync(
            solicitante.Id);

        Assert.Contains(
            Permiso.ConsultarUsuarios,
            efectivos);

        Assert.Equal(
            1,
            await db.PermisosUsuario.CountAsync());
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task RechazarAsync_MotivoVacio_NoModificaSolicitud(
        string? motivo)
    {
        using var database = new TestDatabase();
        using var db = database.CreateContext();

        var administrador = await TestData.AgregarUsuarioAsync(
            db,
            Rol.Administrador);

        var solicitante = await TestData.AgregarUsuarioAsync(db);

        var solicitud = await TestData.AgregarSolicitudAsync(
            db,
            solicitante);

        var service = TestData.CrearSolicitudService(db);

        var resultado = await service.RechazarAsync(
            solicitud.Id,
            administrador.Id,
            new RechazarSolicitudPermisoDto
            {
                MotivoRechazo = motivo!
            });

        Assert.False(resultado.Exito);
        Assert.Equal(
            MotivoResultadoSolicitudPermiso.DatosInvalidos,
            resultado.Motivo);

        var persistida = await db.SolicitudesPermiso
            .AsNoTracking()
            .SingleAsync(s => s.Id == solicitud.Id);

        Assert.Equal(
            EstadoSolicitudPermiso.Pendiente,
            persistida.Estado);
        Assert.Null(persistida.ResueltaPorId);
        Assert.Null(persistida.ResueltaEn);
        Assert.Null(persistida.AplicadaEn);
        Assert.Null(persistida.MotivoRechazo);
    }

    [Fact]
    public async Task RechazarAsync_MotivoValido_RegistraResolutorYFecha()
    {
        using var database = new TestDatabase();
        using var db = database.CreateContext();

        var administrador = await TestData.AgregarUsuarioAsync(
            db,
            Rol.Administrador,
            nombre: "Administrador");

        var solicitante = await TestData.AgregarUsuarioAsync(db);

        var solicitud = await TestData.AgregarSolicitudAsync(
            db,
            solicitante);

        var antes = DateTime.UtcNow;
        var motivo = "  No cumple los requisitos  ";

        var service = TestData.CrearSolicitudService(db);

        var resultado = await service.RechazarAsync(
            solicitud.Id,
            administrador.Id,
            new RechazarSolicitudPermisoDto
            {
                MotivoRechazo = motivo
            });

        var despues = DateTime.UtcNow;

        Assert.True(resultado.Exito);
        Assert.NotNull(resultado.Valor);
        Assert.Equal(
            EstadoSolicitudPermiso.Rechazada,
            resultado.Valor!.Estado);
        Assert.Equal(
            administrador.Id,
            resultado.Valor.ResueltaPorId);
        Assert.Equal(
            administrador.Nombre,
            resultado.Valor.ResueltaPorNombre);
        Assert.NotNull(resultado.Valor.ResueltaEn);
        Assert.InRange(
            resultado.Valor.ResueltaEn!.Value,
            antes,
            despues);
        Assert.Null(resultado.Valor.AplicadaEn);
        Assert.Equal(
            "No cumple los requisitos",
            resultado.Valor.MotivoRechazo);

        var persistida = await db.SolicitudesPermiso
            .AsNoTracking()
            .SingleAsync(s => s.Id == solicitud.Id);

        Assert.Equal(
            EstadoSolicitudPermiso.Rechazada,
            persistida.Estado);
        Assert.Equal(
            "No cumple los requisitos",
            persistida.MotivoRechazo);
    }

    [Fact]
    public async Task RechazarAsync_MotivoDemasiadoLargo_NoModificaSolicitud()
    {
        using var database = new TestDatabase();
        using var db = database.CreateContext();

        var administrador = await TestData.AgregarUsuarioAsync(
            db,
            Rol.Administrador);

        var solicitante = await TestData.AgregarUsuarioAsync(db);

        var solicitud = await TestData.AgregarSolicitudAsync(
            db,
            solicitante);

        var service = TestData.CrearSolicitudService(db);

        var resultado = await service.RechazarAsync(
            solicitud.Id,
            administrador.Id,
            new RechazarSolicitudPermisoDto
            {
                MotivoRechazo = new string('a', 501)
            });

        Assert.False(resultado.Exito);
        Assert.Equal(
            MotivoResultadoSolicitudPermiso.DatosInvalidos,
            resultado.Motivo);

        var persistida = await db.SolicitudesPermiso
            .AsNoTracking()
            .SingleAsync(s => s.Id == solicitud.Id);

        Assert.Equal(
            EstadoSolicitudPermiso.Pendiente,
            persistida.Estado);
        Assert.Null(persistida.MotivoRechazo);
    }

    [Fact]
    public async Task AprobarAsync_SolicitudYaAplicada_NoCambiaEstado()
    {
        using var database = new TestDatabase();
        using var db = database.CreateContext();

        var administrador = await TestData.AgregarUsuarioAsync(
            db,
            Rol.Administrador);

        var solicitante = await TestData.AgregarUsuarioAsync(db);

        var solicitud = await TestData.AgregarSolicitudAsync(
            db,
            solicitante,
            estado: EstadoSolicitudPermiso.Aplicada);

        var service = TestData.CrearSolicitudService(db);

        var resultado = await service.AprobarAsync(
            solicitud.Id,
            administrador.Id);

        Assert.False(resultado.Exito);
        Assert.Equal(
            MotivoResultadoSolicitudPermiso.EstadoInvalido,
            resultado.Motivo);

        var persistida = await db.SolicitudesPermiso
            .AsNoTracking()
            .SingleAsync(s => s.Id == solicitud.Id);

        Assert.Equal(
            EstadoSolicitudPermiso.Aplicada,
            persistida.Estado);
        Assert.Null(persistida.ResueltaPorId);
        Assert.Empty(
            await db.PermisosUsuario.AsNoTracking().ToListAsync());
    }

    [Fact]
    public async Task AprobarAsync_SolicitudRechazada_NoCambiaEstado()
    {
        using var database = new TestDatabase();
        using var db = database.CreateContext();

        var administrador = await TestData.AgregarUsuarioAsync(
            db,
            Rol.Administrador);

        var resolutorOriginal = await TestData.AgregarUsuarioAsync(
            db,
            Rol.Administrador,
            nombre: "Resolutor Original");

        var solicitante = await TestData.AgregarUsuarioAsync(db);

        var solicitud = await TestData.AgregarSolicitudAsync(
            db,
            solicitante,
            estado: EstadoSolicitudPermiso.Rechazada);

        var fechaRechazo = DateTime.UtcNow.AddMinutes(-10);
        var motivoOriginal = "Motivo original de rechazo";

        solicitud.ResueltaPorId = resolutorOriginal.Id;
        solicitud.ResueltaEn = fechaRechazo;
        solicitud.MotivoRechazo = motivoOriginal;

        await db.SaveChangesAsync();

        var service = TestData.CrearSolicitudService(db);

        var resultado = await service.AprobarAsync(
            solicitud.Id,
            administrador.Id);

        Assert.False(resultado.Exito);
        Assert.Equal(
            MotivoResultadoSolicitudPermiso.EstadoInvalido,
            resultado.Motivo);

        var persistida = await db.SolicitudesPermiso
            .AsNoTracking()
            .SingleAsync(s => s.Id == solicitud.Id);

        Assert.Equal(
            EstadoSolicitudPermiso.Rechazada,
            persistida.Estado);
        Assert.Equal(
            resolutorOriginal.Id,
            persistida.ResueltaPorId);
        Assert.Equal(fechaRechazo, persistida.ResueltaEn);
        Assert.Equal(motivoOriginal, persistida.MotivoRechazo);
        Assert.Null(persistida.AplicadaEn);
        Assert.Empty(
            await db.PermisosUsuario.AsNoTracking().ToListAsync());
    }

    [Fact]
    public async Task AprobarAsync_MismaSolicitudDosVeces_NoDuplicaPermiso()
    {
        using var database = new TestDatabase();
        using var db = database.CreateContext();

        var administrador = await TestData.AgregarUsuarioAsync(
            db,
            Rol.Administrador);

        var solicitante = await TestData.AgregarUsuarioAsync(db);

        var solicitud = await TestData.AgregarSolicitudAsync(
            db,
            solicitante);

        var service = TestData.CrearSolicitudService(db);
        var permisoService = new PermisoService(db);

        var primera = await service.AprobarAsync(
            solicitud.Id,
            administrador.Id);

        Assert.True(primera.Exito);
        Assert.NotNull(primera.Valor);
        Assert.Equal(
            EstadoSolicitudPermiso.Aplicada,
            primera.Valor!.Estado);
        Assert.True(
            await permisoService.TienePermisoAsync(
                solicitante.Id,
                Permiso.ConsultarUsuarios));
        Assert.Equal(
            1,
            await db.PermisosUsuario.CountAsync(p =>
                p.UsuarioId == solicitante.Id &&
                p.Permiso == Permiso.ConsultarUsuarios));

        var segunda = await service.AprobarAsync(
            solicitud.Id,
            administrador.Id);

        Assert.False(segunda.Exito);
        Assert.Equal(
            MotivoResultadoSolicitudPermiso.EstadoInvalido,
            segunda.Motivo);

        var persistida = await db.SolicitudesPermiso
            .AsNoTracking()
            .SingleAsync(s => s.Id == solicitud.Id);

        Assert.Equal(
            EstadoSolicitudPermiso.Aplicada,
            persistida.Estado);
        Assert.Equal(
            1,
            await db.PermisosUsuario.CountAsync(p =>
                p.UsuarioId == solicitante.Id &&
                p.Permiso == Permiso.ConsultarUsuarios));
    }

    [Fact]
    public async Task RechazarAsync_SolicitudAprobada_NoCambiaEstado()
    {
        using var database = new TestDatabase();
        using var db = database.CreateContext();

        var administrador = await TestData.AgregarUsuarioAsync(
            db,
            Rol.Administrador);

        var solicitante = await TestData.AgregarUsuarioAsync(db);

        var solicitud = await TestData.AgregarSolicitudAsync(
            db,
            solicitante,
            estado: EstadoSolicitudPermiso.Aprobada);

        var service = TestData.CrearSolicitudService(db);

        var resultado = await service.RechazarAsync(
            solicitud.Id,
            administrador.Id,
            new RechazarSolicitudPermisoDto
            {
                MotivoRechazo = "Intento de transición inválida"
            });

        Assert.False(resultado.Exito);
        Assert.Equal(
            MotivoResultadoSolicitudPermiso.EstadoInvalido,
            resultado.Motivo);

        var persistida = await db.SolicitudesPermiso
            .AsNoTracking()
            .SingleAsync(s => s.Id == solicitud.Id);

        Assert.Equal(
            EstadoSolicitudPermiso.Aprobada,
            persistida.Estado);
        Assert.Null(persistida.ResueltaPorId);
        Assert.Null(persistida.MotivoRechazo);
    }

    [Fact]
    public async Task AprobarAsync_ResolucionPropia_NoModificaSolicitud()
    {
        using var database = new TestDatabase();
        using var db = database.CreateContext();

        var administrador = await TestData.AgregarUsuarioAsync(
            db,
            Rol.Administrador);

        var solicitud = await TestData.AgregarSolicitudAsync(
            db,
            administrador);

        var service = TestData.CrearSolicitudService(db);

        var resultado = await service.AprobarAsync(
            solicitud.Id,
            administrador.Id);

        Assert.False(resultado.Exito);
        Assert.Equal(
            MotivoResultadoSolicitudPermiso.ResolucionPropia,
            resultado.Motivo);

        var persistida = await db.SolicitudesPermiso
            .AsNoTracking()
            .SingleAsync(s => s.Id == solicitud.Id);

        Assert.Equal(
            EstadoSolicitudPermiso.Pendiente,
            persistida.Estado);
        Assert.Null(persistida.ResueltaPorId);
        Assert.Empty(
            await db.PermisosUsuario.AsNoTracking().ToListAsync());
    }

    [Fact]
    public async Task RechazarAsync_ResolucionPropia_NoModificaSolicitud()
    {
        using var database = new TestDatabase();
        using var db = database.CreateContext();

        var administrador = await TestData.AgregarUsuarioAsync(
            db,
            Rol.Administrador);

        var solicitud = await TestData.AgregarSolicitudAsync(
            db,
            administrador);

        var service = TestData.CrearSolicitudService(db);

        var resultado = await service.RechazarAsync(
            solicitud.Id,
            administrador.Id,
            new RechazarSolicitudPermisoDto
            {
                MotivoRechazo = "Intento de resolución propia"
            });

        Assert.False(resultado.Exito);
        Assert.Equal(
            MotivoResultadoSolicitudPermiso.ResolucionPropia,
            resultado.Motivo);

        var persistida = await db.SolicitudesPermiso
            .AsNoTracking()
            .SingleAsync(s => s.Id == solicitud.Id);

        Assert.Equal(
            EstadoSolicitudPermiso.Pendiente,
            persistida.Estado);
        Assert.Null(persistida.ResueltaPorId);
        Assert.Null(persistida.MotivoRechazo);
    }

    [Theory]
    [InlineData(Permiso.GestionarUsuarios)]
    [InlineData(Permiso.RestablecerContrasenas)]
    public async Task CrearAsync_PermisoAdministrativo_NoEsSolicitable(
        Permiso permiso)
    {
        using var database = new TestDatabase();
        using var db = database.CreateContext();

        var solicitante = await TestData.AgregarUsuarioAsync(db);

        Assert.False(PermisosSolicitables.Contiene(permiso));

        var service = TestData.CrearSolicitudService(db);

        var resultado = await service.CrearAsync(
            solicitante.Id,
            new CrearSolicitudPermisoDto
            {
                Permiso = permiso
            });

        Assert.False(resultado.Exito);
        Assert.Equal(
            MotivoResultadoSolicitudPermiso.PermisoNoSolicitable,
            resultado.Motivo);

        Assert.Empty(
            await db.SolicitudesPermiso.AsNoTracking().ToListAsync());
    }

    [Fact]
    public async Task CrearAsync_EnumInvalido_DevuelvePermisoInexistente()
    {
        using var database = new TestDatabase();
        using var db = database.CreateContext();

        var solicitante = await TestData.AgregarUsuarioAsync(db);

        var service = TestData.CrearSolicitudService(db);

        var resultado = await service.CrearAsync(
            solicitante.Id,
            new CrearSolicitudPermisoDto
            {
                Permiso = (Permiso)999
            });

        Assert.False(resultado.Exito);
        Assert.Equal(
            MotivoResultadoSolicitudPermiso.PermisoInexistente,
            resultado.Motivo);

        Assert.Empty(
            await db.SolicitudesPermiso.AsNoTracking().ToListAsync());
    }

    [Fact]
    public async Task CrearAsync_UsuarioInexistente_DevuelveUsuarioNoEncontrado()
    {
        using var database = new TestDatabase();
        using var db = database.CreateContext();

        var service = TestData.CrearSolicitudService(db);

        var resultado = await service.CrearAsync(
            Guid.NewGuid(),
            new CrearSolicitudPermisoDto
            {
                Permiso = Permiso.ConsultarUsuarios
            });

        Assert.False(resultado.Exito);
        Assert.Equal(
            MotivoResultadoSolicitudPermiso.UsuarioNoEncontrado,
            resultado.Motivo);

        Assert.Empty(
            await db.SolicitudesPermiso.AsNoTracking().ToListAsync());
    }

    [Fact]
    public async Task AprobarAsync_UsuarioConPermisoPrevio_NoDuplicaConcesion()
    {
        using var database = new TestDatabase();
        using var db = database.CreateContext();

        var administrador = await TestData.AgregarUsuarioAsync(
            db,
            Rol.Administrador);

        var solicitante = await TestData.AgregarUsuarioAsync(db);

        await TestData.AgregarPermisoAsync(
            db,
            solicitante,
            Permiso.ConsultarUsuarios);

        var solicitud = await TestData.AgregarSolicitudAsync(
            db,
            solicitante);

        var service = TestData.CrearSolicitudService(db);

        var resultado = await service.AprobarAsync(
            solicitud.Id,
            administrador.Id);

        Assert.True(resultado.Exito);
        Assert.Equal(
            1,
            await db.PermisosUsuario.CountAsync());
    }

    [Fact]
    public async Task AprobarAsync_UsuarioInactivo_NoDejaCambiosParciales()
    {
        using var database = new TestDatabase();
        using var db = database.CreateContext();

        var administrador = await TestData.AgregarUsuarioAsync(
            db,
            Rol.Administrador);

        var solicitante = await TestData.AgregarUsuarioAsync(db);

        var solicitud = await TestData.AgregarSolicitudAsync(
            db,
            solicitante);

        solicitante.Activo = false;

        await db.SaveChangesAsync();

        var service = TestData.CrearSolicitudService(db);

        var resultado = await service.AprobarAsync(
            solicitud.Id,
            administrador.Id);

        Assert.False(resultado.Exito);
        Assert.Equal(
            MotivoResultadoSolicitudPermiso.UsuarioInactivo,
            resultado.Motivo);

        var persistida = await db.SolicitudesPermiso
            .AsNoTracking()
            .SingleAsync(s => s.Id == solicitud.Id);

        Assert.Equal(
            EstadoSolicitudPermiso.Pendiente,
            persistida.Estado);
        Assert.Null(persistida.ResueltaPorId);
        Assert.Null(persistida.ResueltaEn);
        Assert.Null(persistida.AplicadaEn);
        Assert.Empty(
            await db.PermisosUsuario.AsNoTracking().ToListAsync());
    }

    [Fact]
    public async Task AprobarAsync_FalloDePersistencia_NoDejaCambiosParciales()
    {
        var interceptor =
            new FailingSaveChangesInterceptor();

        using var database = new TestDatabase();
        using var db = database.CreateContext(interceptor);

        var administrador = await TestData.AgregarUsuarioAsync(
            db,
            Rol.Administrador);

        var solicitante = await TestData.AgregarUsuarioAsync(db);

        var solicitud = await TestData.AgregarSolicitudAsync(
            db,
            solicitante);

        interceptor.Fail = true;

        var service = TestData.CrearSolicitudService(db);

        var resultado = await service.AprobarAsync(
            solicitud.Id,
            administrador.Id);

        Assert.False(resultado.Exito);
        Assert.Equal(
            MotivoResultadoSolicitudPermiso.ErrorPersistencia,
            resultado.Motivo);

        var persistida = await db.SolicitudesPermiso
            .AsNoTracking()
            .SingleAsync(s => s.Id == solicitud.Id);

        Assert.Equal(
            EstadoSolicitudPermiso.Pendiente,
            persistida.Estado);
        Assert.Null(persistida.ResueltaPorId);
        Assert.Null(persistida.ResueltaEn);
        Assert.Null(persistida.AplicadaEn);
        Assert.Empty(
            await db.PermisosUsuario.AsNoTracking().ToListAsync());
    }
}
