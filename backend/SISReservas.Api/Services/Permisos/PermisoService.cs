using Microsoft.EntityFrameworkCore;
using SISReservas.Api.Data;
using SISReservas.Api.Models;
using SISReservas.Api.Models.Enums;

namespace SISReservas.Api.Services;

public enum MotivoResultadoConcesionPermiso
{
    Exito = 0,
    PermisoInexistente = 1,
    UsuarioNoEncontrado = 2,
    UsuarioInactivo = 3,
    PermisoNoSolicitable = 4
}

public sealed class ResultadoConcesionPermiso
{
    public MotivoResultadoConcesionPermiso Motivo { get; }

    public string Mensaje { get; }

    public UsuarioPermiso? PermisoUsuario { get; }

    public bool Exito => Motivo == MotivoResultadoConcesionPermiso.Exito;

    private ResultadoConcesionPermiso(
        MotivoResultadoConcesionPermiso motivo,
        string mensaje,
        UsuarioPermiso? permisoUsuario)
    {
        Motivo = motivo;
        Mensaje = mensaje;
        PermisoUsuario = permisoUsuario;
    }

    public static ResultadoConcesionPermiso Ok(
        UsuarioPermiso permisoUsuario)
    {
        return new ResultadoConcesionPermiso(
            MotivoResultadoConcesionPermiso.Exito,
            string.Empty,
            permisoUsuario);
    }

    public static ResultadoConcesionPermiso Falla(
        MotivoResultadoConcesionPermiso motivo,
        string mensaje)
    {
        return new ResultadoConcesionPermiso(
            motivo,
            mensaje,
            null);
    }
}

public class PermisoService
{
    private readonly SISReservasDbContext _db;

    public PermisoService(SISReservasDbContext db)
    {
        _db = db;
    }

    public async Task<List<Permiso>> ObtenerEfectivosAsync(Guid usuarioId)
    {
        var usuario = await _db.Usuarios
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == usuarioId);

        if (usuario is null || !usuario.Activo)
            return [];

        if (usuario.Rol == Rol.Administrador)
            return Enum.GetValues<Permiso>().ToList();

        var permisos = await _db.PermisosUsuario
            .AsNoTracking()
            .Where(p => p.UsuarioId == usuarioId)
            .Select(p => p.Permiso)
            .Distinct()
            .ToListAsync();

        return permisos
            .Where(p =>
                Enum.IsDefined(typeof(Permiso), p) &&
                PermisosSolicitables.Contiene(p))
            .ToList();
    }

    public async Task<bool> TienePermisoAsync(
        Guid usuarioId,
        Permiso permiso)
    {
        if (!Enum.IsDefined(typeof(Permiso), permiso))
            return false;

        var usuario = await _db.Usuarios
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == usuarioId);

        if (usuario is null || !usuario.Activo)
            return false;

        if (usuario.Rol == Rol.Administrador)
            return true;

        if (!PermisosSolicitables.Contiene(permiso))
            return false;

        return await _db.PermisosUsuario
            .AnyAsync(p =>
                p.UsuarioId == usuarioId &&
                p.Permiso == permiso);
    }

    public async Task<ResultadoConcesionPermiso> PrepararConcesionAsync(
        Guid usuarioId,
        Permiso permiso)
    {
        if (!Enum.IsDefined(typeof(Permiso), permiso))
        {
            return ResultadoConcesionPermiso.Falla(
                MotivoResultadoConcesionPermiso.PermisoInexistente,
                "El permiso solicitado no existe.");
        }

        if (!PermisosSolicitables.Contiene(permiso))
        {
            return ResultadoConcesionPermiso.Falla(
                MotivoResultadoConcesionPermiso.PermisoNoSolicitable,
                "El permiso no está habilitado para concesión mediante solicitudes.");
        }

        var usuario = await _db.Usuarios
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == usuarioId);

        if (usuario is null)
        {
            return ResultadoConcesionPermiso.Falla(
                MotivoResultadoConcesionPermiso.UsuarioNoEncontrado,
                "El usuario destinatario no existe.");
        }

        if (!usuario.Activo)
        {
            return ResultadoConcesionPermiso.Falla(
                MotivoResultadoConcesionPermiso.UsuarioInactivo,
                "El usuario destinatario no está activo.");
        }

        var existente = await _db.PermisosUsuario
            .FirstOrDefaultAsync(p =>
                p.UsuarioId == usuarioId &&
                p.Permiso == permiso);

        if (existente is not null)
            return ResultadoConcesionPermiso.Ok(existente);

        var nuevo = new UsuarioPermiso
        {
            UsuarioId = usuarioId,
            Permiso = permiso
        };

        _db.PermisosUsuario.Add(nuevo);

        return ResultadoConcesionPermiso.Ok(nuevo);
    }

    public async Task<ResultadoConcesionPermiso> ConcederAsync(
        Guid usuarioId,
        Permiso permiso)
    {
        var resultado = await PrepararConcesionAsync(
            usuarioId,
            permiso);

        if (!resultado.Exito)
            return resultado;

        try
        {
            await _db.SaveChangesAsync();
        }
        catch
        {
            if (resultado.PermisoUsuario is not null &&
                _db.Entry(resultado.PermisoUsuario).State == EntityState.Added)
            {
                _db.Entry(resultado.PermisoUsuario).State =
                    EntityState.Detached;
            }

            throw;
        }

        return resultado;
    }
}
