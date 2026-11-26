using Microsoft.EntityFrameworkCore;
using SISReservas.Api.Data;
using SISReservas.Api.Models;
using SISReservas.Api.Models.Enums;
using SISReservas.Api.DTOs.Notificaciones;

namespace SISReservas.Api.Services;

public class NotificacionService(SISReservasDbContext db)
{
    private static readonly SemaphoreSlim LecturaNoRelacional = new(1, 1);

    public async Task PrepararAsync(IEnumerable<Guid> destinatarios, string asunto, string mensaje)
    {
        var ids = destinatarios.Distinct().ToArray();
        var usuarios = await db.Usuarios.Where(u => ids.Contains(u.Id) && u.Activo).ToListAsync();
        foreach (var usuario in usuarios)
        {
            db.Notificaciones.Add(new Notificacion { UsuarioId = usuario.Id, Asunto = asunto, Mensaje = mensaje });
            if (usuario.CorreoNotificacionesHabilitado)
                db.CorreosEnCola.Add(new CorreoEnCola
                {
                    UsuarioId = usuario.Id, Destinatario = usuario.Email, Asunto = asunto, Cuerpo = mensaje
                });
        }
        // El servicio originador guarda negocio, bandeja y correo en un único SaveChanges.
    }

    public async Task SolicitudCreadaAsync(Guid solicitud)
    {
        var administradores = await db.Usuarios.Where(u => u.Activo && u.Rol == Rol.Administrador)
            .Select(u => u.Id).ToListAsync();
        await PrepararAsync(administradores, "Nueva solicitud de permiso", $"Solicitud {solicitud} pendiente de resolución.");
    }

    public void DescartarPendientes()
    {
        foreach (var entry in db.ChangeTracker.Entries().Where(e => e.State == EntityState.Added
            && (e.Entity is Notificacion || e.Entity is CorreoEnCola)).ToList())
            entry.State = EntityState.Detached;
    }

    public async Task<Usuario> ExigirUsuarioAsync(Guid usuario)
    {
        var cuenta = await db.Usuarios.FirstOrDefaultAsync(u => u.Id == usuario && u.Activo);
        if (cuenta is null || cuenta.Rol is not (Rol.Administrador or Rol.Estandar))
            throw new UnauthorizedAccessException("Usuario inactivo o rol no permitido.");
        return cuenta;
    }

    public async Task<BandejaDto> ConsultarAsync(Guid usuario)
    {
        await ExigirUsuarioAsync(usuario);
        var consulta = db.Notificaciones.AsNoTracking().Where(n => n.UsuarioId == usuario);
        var noLeidas = await consulta.CountAsync(n => n.LeidaEn == null);
        var filas = await consulta.OrderByDescending(n => n.CreadaEn).ThenBy(n => n.Id)
            .Select(n => new NotificacionDto(n.Id, n.Asunto, n.Mensaje, n.CreadaEn, n.LeidaEn)).ToListAsync();
        return new(noLeidas, filas);
    }

    public async Task<bool> LeerAsync(Guid usuario, Guid id)
    {
        await ExigirUsuarioAsync(usuario);
        var propias = db.Notificaciones.Where(n => n.Id == id && n.UsuarioId == usuario);
        if (db.Database.IsRelational())
        {
            // Compare-and-set en SQL: solo quien encuentra LeidaEn NULL escribe la fecha.
            var fecha = DateTime.UtcNow;
            var modificadas = await propias.Where(n => n.LeidaEn == null)
                .ExecuteUpdateAsync(s => s.SetProperty(n => n.LeidaEn, (DateTime?)fecha));
            return modificadas > 0 || await propias.AnyAsync();
        }

        // InMemory carece de UPDATE condicional; conserva la misma semántica en las pruebas.
        await LecturaNoRelacional.WaitAsync();
        try
        {
            var notificacion = await propias.FirstOrDefaultAsync();
            if (notificacion is null) return false;
            await db.Entry(notificacion).ReloadAsync();
            if (notificacion.LeidaEn is null)
            {
                notificacion.LeidaEn = DateTime.UtcNow;
                await db.SaveChangesAsync();
            }
            return true;
        }
        finally { LecturaNoRelacional.Release(); }
    }

    public async Task<PreferenciasDto> PreferenciasAsync(Guid usuario, bool? correo = null)
    {
        var cuenta = await ExigirUsuarioAsync(usuario);
        if (correo.HasValue)
        {
            cuenta.CorreoNotificacionesHabilitado = correo.Value;
            await db.SaveChangesAsync();
        }
        return new(cuenta.CorreoNotificacionesHabilitado);
    }
}
