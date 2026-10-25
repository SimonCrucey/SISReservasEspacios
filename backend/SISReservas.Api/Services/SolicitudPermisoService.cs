using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using SISReservas.Api.Data;
using SISReservas.Api.DTOs.Permisos;
using SISReservas.Api.Models;
using SISReservas.Api.Models.Enums;

namespace SISReservas.Api.Services;

public enum MotivoResultadoSolicitudPermiso
{
    Exito = 0,
    DatosInvalidos = 1,
    UsuarioNoEncontrado = 2,
    RolNoPermitido = 3,
    PermisoInexistente = 4,
    SolicitudNoEncontrada = 5,
    EstadoInvalido = 6,
    ResolucionPropia = 7,
    ConcesionDuplicada = 8,
    PermisoNoSolicitable = 9,
    UsuarioInactivo = 10,
    ConflictoConcurrencia = 11,
    ErrorPersistencia = 12
}

public sealed class ResultadoSolicitudPermiso<T>
{
    public MotivoResultadoSolicitudPermiso Motivo { get; }

    public string Mensaje { get; }

    public T? Valor { get; }

    public bool Exito => Motivo == MotivoResultadoSolicitudPermiso.Exito;

    private ResultadoSolicitudPermiso(
        MotivoResultadoSolicitudPermiso motivo,
        string mensaje,
        T? valor)
    {
        Motivo = motivo;
        Mensaje = mensaje;
        Valor = valor;
    }

    public static ResultadoSolicitudPermiso<T> Ok(T valor)
    {
        return new ResultadoSolicitudPermiso<T>(
            MotivoResultadoSolicitudPermiso.Exito,
            string.Empty,
            valor);
    }

    public static ResultadoSolicitudPermiso<T> Falla(
        MotivoResultadoSolicitudPermiso motivo,
        string mensaje)
    {
        return new ResultadoSolicitudPermiso<T>(
            motivo,
            mensaje,
            default);
    }
}

public class SolicitudPermisoService
{
    private const int LongitudMaximaMotivo = 500;

    private readonly SISReservasDbContext _db;
    private readonly PermisoService _permisoService;

    public SolicitudPermisoService(
        SISReservasDbContext db,
        PermisoService permisoService)
    {
        _db = db;
        _permisoService = permisoService;
    }

    public async Task<ResultadoSolicitudPermiso<SolicitudPermisoResponseDto>>
        CrearAsync(
            Guid solicitanteId,
            CrearSolicitudPermisoDto request)
    {
        if (request is null ||
            !Enum.IsDefined(typeof(Permiso), request.Permiso))
        {
            return ResultadoSolicitudPermiso<SolicitudPermisoResponseDto>
                .Falla(
                    MotivoResultadoSolicitudPermiso.PermisoInexistente,
                    "El permiso solicitado no existe.");
        }

        if (!PermisosSolicitables.Contiene(request.Permiso))
        {
            return ResultadoSolicitudPermiso<SolicitudPermisoResponseDto>
                .Falla(
                    MotivoResultadoSolicitudPermiso.PermisoNoSolicitable,
                    "El permiso solicitado no está habilitado para solicitudes de usuarios Estándar.");
        }

        var solicitante = await _db.Usuarios
            .FirstOrDefaultAsync(u => u.Id == solicitanteId);

        if (solicitante is null)
        {
            return ResultadoSolicitudPermiso<SolicitudPermisoResponseDto>
                .Falla(
                    MotivoResultadoSolicitudPermiso.UsuarioNoEncontrado,
                    "El usuario solicitante no existe.");
        }

        if (solicitante.Rol != Rol.Estandar)
        {
            return ResultadoSolicitudPermiso<SolicitudPermisoResponseDto>
                .Falla(
                    MotivoResultadoSolicitudPermiso.RolNoPermitido,
                    "Solo un usuario Estándar puede solicitar permisos.");
        }

        var solicitud = new SolicitudPermiso
        {
            SolicitanteId = solicitanteId,
            Permiso = request.Permiso
        };

        _db.SolicitudesPermiso.Add(solicitud);

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            _db.Entry(solicitud).State = EntityState.Detached;

            return ResultadoSolicitudPermiso<SolicitudPermisoResponseDto>
                .Falla(
                    MotivoResultadoSolicitudPermiso.ErrorPersistencia,
                    "No se pudo registrar la solicitud.");
        }

        return ResultadoSolicitudPermiso<SolicitudPermisoResponseDto>
            .Ok(ToDto(solicitud, solicitante.Nombre));
    }

    public async Task<ResultadoSolicitudPermiso<
        List<SolicitudPermisoResponseDto>>> ObtenerPendientesAsync(
            Guid usuarioId)
    {
        var usuario = await _db.Usuarios
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == usuarioId);

        if (usuario is null)
        {
            return ResultadoSolicitudPermiso<List<SolicitudPermisoResponseDto>>
                .Falla(
                    MotivoResultadoSolicitudPermiso.UsuarioNoEncontrado,
                    "El usuario no existe.");
        }

        if (usuario.Rol != Rol.Administrador)
        {
            return ResultadoSolicitudPermiso<List<SolicitudPermisoResponseDto>>
                .Falla(
                    MotivoResultadoSolicitudPermiso.RolNoPermitido,
                    "Solo un Administrador puede consultar las solicitudes pendientes.");
        }

        var solicitudes = await _db.SolicitudesPermiso
            .AsNoTracking()
            .Include(s => s.Solicitante)
            .Include(s => s.ResueltaPor)
            .Where(s => s.Estado == EstadoSolicitudPermiso.Pendiente)
            .OrderBy(s => s.CreadaEn)
            .ToListAsync();

        return ResultadoSolicitudPermiso<List<SolicitudPermisoResponseDto>>
            .Ok(solicitudes.Select(s => ToDto(s)).ToList());
    }

    public async Task<ResultadoSolicitudPermiso<
        List<SolicitudPermisoResponseDto>>> ObtenerMiasAsync(
            Guid solicitanteId)
    {
        var solicitante = await _db.Usuarios
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == solicitanteId);

        if (solicitante is null)
        {
            return ResultadoSolicitudPermiso<List<SolicitudPermisoResponseDto>>
                .Falla(
                    MotivoResultadoSolicitudPermiso.UsuarioNoEncontrado,
                    "El usuario no existe.");
        }

        var solicitudes = await _db.SolicitudesPermiso
            .AsNoTracking()
            .Include(s => s.Solicitante)
            .Include(s => s.ResueltaPor)
            .Where(s => s.SolicitanteId == solicitanteId)
            .OrderByDescending(s => s.CreadaEn)
            .ToListAsync();

        return ResultadoSolicitudPermiso<List<SolicitudPermisoResponseDto>>
            .Ok(solicitudes.Select(s => ToDto(s)).ToList());
    }

    public async Task<ResultadoSolicitudPermiso<SolicitudPermisoResponseDto>>
        AprobarAsync(
            Guid solicitudId,
            Guid resolutorId)
    {
        var solicitud = await _db.SolicitudesPermiso
            .Include(s => s.Solicitante)
            .FirstOrDefaultAsync(s => s.Id == solicitudId);

        if (solicitud is null)
        {
            return ResultadoSolicitudPermiso<SolicitudPermisoResponseDto>
                .Falla(
                    MotivoResultadoSolicitudPermiso.SolicitudNoEncontrada,
                    "La solicitud no existe.");
        }

        var resolutor = await _db.Usuarios
            .FirstOrDefaultAsync(u => u.Id == resolutorId);

        if (resolutor is null)
        {
            return ResultadoSolicitudPermiso<SolicitudPermisoResponseDto>
                .Falla(
                    MotivoResultadoSolicitudPermiso.UsuarioNoEncontrado,
                    "El usuario resolutor no existe.");
        }

        if (resolutor.Rol != Rol.Administrador)
        {
            return ResultadoSolicitudPermiso<SolicitudPermisoResponseDto>
                .Falla(
                    MotivoResultadoSolicitudPermiso.RolNoPermitido,
                    "Solo un Administrador puede aprobar solicitudes.");
        }

        if (solicitud.SolicitanteId != resolutorId)
        {
            return ResultadoSolicitudPermiso<SolicitudPermisoResponseDto>
                .Falla(
                    MotivoResultadoSolicitudPermiso.ResolucionPropia,
                    "No puedes aprobar tu propia solicitud.");
        }

        if (solicitud.Estado != EstadoSolicitudPermiso.Pendiente)
        {
            return ResultadoSolicitudPermiso<SolicitudPermisoResponseDto>
                .Falla(
                    MotivoResultadoSolicitudPermiso.EstadoInvalido,
                    "Solo se pueden aprobar solicitudes Pendientes.");
        }

        if (!Enum.IsDefined(typeof(Permiso), solicitud.Permiso))
        {
            return ResultadoSolicitudPermiso<SolicitudPermisoResponseDto>
                .Falla(
                    MotivoResultadoSolicitudPermiso.PermisoInexistente,
                    "El permiso solicitado no existe.");
        }

        if (!PermisosSolicitables.Contiene(solicitud.Permiso))
        {
            return ResultadoSolicitudPermiso<SolicitudPermisoResponseDto>
                .Falla(
                    MotivoResultadoSolicitudPermiso.PermisoNoSolicitable,
                    "El permiso solicitado no está habilitado para solicitudes de usuarios Estándar.");
        }

        var estadoOriginal = solicitud.Estado;
        var resueltaPorIdOriginal = solicitud.ResueltaPorId;
        var resueltaEnOriginal = solicitud.ResueltaEn;
        var aplicadaEnOriginal = solicitud.AplicadaEn;
        var motivoRechazoOriginal = solicitud.MotivoRechazo;

        if (!IntentarTransicionar(
                solicitud,
                EstadoSolicitudPermiso.Aprobada))
        {
            return ResultadoSolicitudPermiso<SolicitudPermisoResponseDto>
                .Falla(
                    MotivoResultadoSolicitudPermiso.EstadoInvalido,
                    "La transición a Aprobada no es válida.");
        }

        solicitud.ResueltaPorId = resolutorId;
        solicitud.ResueltaEn = DateTime.UtcNow;

        var concesion = await _permisoService.PrepararConcesionAsync(
            solicitud.SolicitanteId,
            solicitud.Permiso);

        if (!concesion.Exito)
        {
            RestaurarSolicitud(
                solicitud,
                estadoOriginal,
                resueltaPorIdOriginal,
                resueltaEnOriginal,
                aplicadaEnOriginal,
                motivoRechazoOriginal);

            return ResultadoSolicitudPermiso<SolicitudPermisoResponseDto>
                .Falla(
                    MapearMotivoConcesion(concesion.Motivo),
                    concesion.Mensaje);
        }

        if (!IntentarTransicionar(
                solicitud,
                EstadoSolicitudPermiso.Aplicada))
        {
            RestaurarSolicitud(
                solicitud,
                estadoOriginal,
                resueltaPorIdOriginal,
                resueltaEnOriginal,
                aplicadaEnOriginal,
                motivoRechazoOriginal);

            DescartarConcesionNueva(concesion);

            return ResultadoSolicitudPermiso<SolicitudPermisoResponseDto>
                .Falla(
                    MotivoResultadoSolicitudPermiso.EstadoInvalido,
                    "La transición a Aplicada no es válida.");
        }

        solicitud.AplicadaEn = DateTime.UtcNow;

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            RestaurarSolicitud(
                solicitud,
                estadoOriginal,
                resueltaPorIdOriginal,
                resueltaEnOriginal,
                aplicadaEnOriginal,
                motivoRechazoOriginal);

            DescartarConcesionNueva(concesion);

            return ResultadoSolicitudPermiso<SolicitudPermisoResponseDto>
                .Falla(
                    MotivoResultadoSolicitudPermiso.ConflictoConcurrencia,
                    "La solicitud ya fue resuelta por otro Administrador.");
        }
        catch (DbUpdateException exception)
        {
            RestaurarSolicitud(
                solicitud,
                estadoOriginal,
                resueltaPorIdOriginal,
                resueltaEnOriginal,
                aplicadaEnOriginal,
                motivoRechazoOriginal);

            DescartarConcesionNueva(concesion);

            if (EsConflictoDeUnicidad(exception))
            {
                return ResultadoSolicitudPermiso<SolicitudPermisoResponseDto>
                    .Falla(
                        MotivoResultadoSolicitudPermiso.ConcesionDuplicada,
                        "No se pudo conceder el permiso porque ya existe una concesión equivalente.");
            }

            return ResultadoSolicitudPermiso<SolicitudPermisoResponseDto>
                .Falla(
                    MotivoResultadoSolicitudPermiso.ErrorPersistencia,
                    "No se pudo guardar la resolución de la solicitud.");
        }

        return ResultadoSolicitudPermiso<SolicitudPermisoResponseDto>
            .Ok(ToDto(solicitud, null, resolutor.Nombre));
    }

    public async Task<ResultadoSolicitudPermiso<SolicitudPermisoResponseDto>>
        RechazarAsync(
            Guid solicitudId,
            Guid resolutorId,
            RechazarSolicitudPermisoDto request)
    {
        if (request is null ||
            string.IsNullOrWhiteSpace(request.MotivoRechazo))
        {
            return ResultadoSolicitudPermiso<SolicitudPermisoResponseDto>
                .Falla(
                    MotivoResultadoSolicitudPermiso.DatosInvalidos,
                    "El motivo de rechazo es obligatorio.");
        }

        var motivo = request.MotivoRechazo.Trim();

        if (motivo.Length > LongitudMaximaMotivo)
        {
            return ResultadoSolicitudPermiso<SolicitudPermisoResponseDto>
                .Falla(
                    MotivoResultadoSolicitudPermiso.DatosInvalidos,
                    $"El motivo de rechazo no puede superar {LongitudMaximaMotivo} caracteres.");
        }

        var solicitud = await _db.SolicitudesPermiso
            .Include(s => s.Solicitante)
            .FirstOrDefaultAsync(s => s.Id == solicitudId);

        if (solicitud is null)
        {
            return ResultadoSolicitudPermiso<SolicitudPermisoResponseDto>
                .Falla(
                    MotivoResultadoSolicitudPermiso.SolicitudNoEncontrada,
                    "La solicitud no existe.");
        }

        var resolutor = await _db.Usuarios
            .FirstOrDefaultAsync(u => u.Id == resolutorId);

        if (resolutor is null)
        {
            return ResultadoSolicitudPermiso<SolicitudPermisoResponseDto>
                .Falla(
                    MotivoResultadoSolicitudPermiso.UsuarioNoEncontrado,
                    "El usuario resolutor no existe.");
        }

        if (resolutor.Rol != Rol.Administrador)
        {
            return ResultadoSolicitudPermiso<SolicitudPermisoResponseDto>
                .Falla(
                    MotivoResultadoSolicitudPermiso.RolNoPermitido,
                    "Solo un Administrador puede rechazar solicitudes.");
        }

        if (solicitud.SolicitanteId == resolutorId)
        {
            return ResultadoSolicitudPermiso<SolicitudPermisoResponseDto>
                .Falla(
                    MotivoResultadoSolicitudPermiso.ResolucionPropia,
                    "No puedes rechazar tu propia solicitud.");
        }

        if (solicitud.Estado != EstadoSolicitudPermiso.Pendiente)
        {
            return ResultadoSolicitudPermiso<SolicitudPermisoResponseDto>
                .Falla(
                    MotivoResultadoSolicitudPermiso.EstadoInvalido,
                    "Solo se pueden rechazar solicitudes Pendientes.");
        }

        var estadoOriginal = solicitud.Estado;
        var resueltaPorIdOriginal = solicitud.ResueltaPorId;
        var resueltaEnOriginal = solicitud.ResueltaEn;
        var aplicadaEnOriginal = solicitud.AplicadaEn;
        var motivoRechazoOriginal = solicitud.MotivoRechazo;

        if (!IntentarTransicionar(
                solicitud,
                EstadoSolicitudPermiso.Rechazada))
        {
            return ResultadoSolicitudPermiso<SolicitudPermisoResponseDto>
                .Falla(
                    MotivoResultadoSolicitudPermiso.EstadoInvalido,
                    "La transición a Rechazada no es válida.");
        }

        solicitud.ResueltaPorId = resolutorId;
        solicitud.ResueltaEn = DateTime.UtcNow;
        solicitud.MotivoRechazo = motivo;

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            RestaurarSolicitud(
                solicitud,
                estadoOriginal,
                resueltaPorIdOriginal,
                resueltaEnOriginal,
                aplicadaEnOriginal,
                motivoRechazoOriginal);

            return ResultadoSolicitudPermiso<SolicitudPermisoResponseDto>
                .Falla(
                    MotivoResultadoSolicitudPermiso.ConflictoConcurrencia,
                    "La solicitud ya fue resuelta por otro Administrador.");
        }
        catch (DbUpdateException)
        {
            RestaurarSolicitud(
                solicitud,
                estadoOriginal,
                resueltaPorIdOriginal,
                resueltaEnOriginal,
                aplicadaEnOriginal,
                motivoRechazoOriginal);

            return ResultadoSolicitudPermiso<SolicitudPermisoResponseDto>
                .Falla(
                    MotivoResultadoSolicitudPermiso.ErrorPersistencia,
                    "No se pudo guardar la resolución de la solicitud.");
        }

        return ResultadoSolicitudPermiso<SolicitudPermisoResponseDto>
            .Ok(ToDto(solicitud, null, resolutor.Nombre));
    }

    private static bool IntentarTransicionar(
        SolicitudPermiso solicitud,
        EstadoSolicitudPermiso nuevoEstado)
    {
        try
        {
            TransicionesSolicitudPermiso.AsegurarPermitida(
                solicitud.Estado,
                nuevoEstado);
        }
        catch (InvalidOperationException)
        {
            return false;
        }

        solicitud.Estado = nuevoEstado;

        return true;
    }

    private void RestaurarSolicitud(
        SolicitudPermiso solicitud,
        EstadoSolicitudPermiso estado,
        Guid? resueltaPorId,
        DateTime? resueltaEn,
        DateTime? aplicadaEn,
        string? motivoRechazo)
    {
        solicitud.Estado = estado;
        solicitud.ResueltaPorId = resueltaPorId;
        solicitud.ResueltaEn = resueltaEn;
        solicitud.AplicadaEn = aplicadaEn;
        solicitud.MotivoRechazo = motivoRechazo;

        _db.Entry(solicitud).State = EntityState.Unchanged;
    }

    private void DescartarConcesionNueva(
        ResultadoConcesionPermiso concesion)
    {
        if (!concesion.Exito ||
            concesion.PermisoUsuario is null)
        {
            return;
        }

        var entry = _db.Entry(concesion.PermisoUsuario);

        if (entry.State == EntityState.Added)
            entry.State = EntityState.Detached;
    }

    private static MotivoResultadoSolicitudPermiso MapearMotivoConcesion(
        MotivoResultadoConcesionPermiso motivo)
    {
        return motivo switch
        {
            MotivoResultadoConcesionPermiso.PermisoInexistente =>
                MotivoResultadoSolicitudPermiso.PermisoInexistente,
            MotivoResultadoConcesionPermiso.UsuarioNoEncontrado =>
                MotivoResultadoSolicitudPermiso.UsuarioNoEncontrado,
            MotivoResultadoConcesionPermiso.UsuarioInactivo =>
                MotivoResultadoSolicitudPermiso.UsuarioInactivo,
            MotivoResultadoConcesionPermiso.PermisoNoSolicitable =>
                MotivoResultadoSolicitudPermiso.PermisoNoSolicitable,
            _ => MotivoResultadoSolicitudPermiso.ErrorPersistencia
        };
    }

    private static bool EsConflictoDeUnicidad(
        DbUpdateException exception)
    {
        return exception.InnerException is SqlException sqlException &&
            (sqlException.Number == 2601 || sqlException.Number == 2627);
    }

    private static SolicitudPermisoResponseDto ToDto(
        SolicitudPermiso solicitud,
        string? solicitanteNombre = null,
        string? resolutorNombre = null)
    {
        return new SolicitudPermisoResponseDto
        {
            Id = solicitud.Id,
            SolicitanteId = solicitud.SolicitanteId,
            SolicitanteNombre = solicitanteNombre
                ?? solicitud.Solicitante?.Nombre
                ?? string.Empty,
            Permiso = solicitud.Permiso,
            Estado = solicitud.Estado,
            CreadaEn = solicitud.CreadaEn,
            ResueltaPorId = solicitud.ResueltaPorId,
            ResueltaPorNombre = resolutorNombre ?? solicitud.ResueltaPor?.Nombre,
            ResueltaEn = solicitud.ResueltaEn,
            AplicadaEn = solicitud.AplicadaEn,
            MotivoRechazo = solicitud.MotivoRechazo
        };
    }
}
