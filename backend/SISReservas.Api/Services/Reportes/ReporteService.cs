using Microsoft.EntityFrameworkCore;
using SISReservas.Api.Data;
using SISReservas.Api.DTOs.Reportes;
using SISReservas.Api.Models.Enums;

namespace SISReservas.Api.Services;

public class ReporteService(SISReservasDbContext db)
{
    private async Task<bool> PrepararAsync(Guid usuario, DateTime inicio, DateTime fin)
    {
        var cuenta = await new NotificacionService(db).ExigirUsuarioAsync(usuario);
        if (inicio.Kind != DateTimeKind.Utc || fin.Kind != DateTimeKind.Utc || inicio >= fin
            || (fin - inicio).TotalDays > 3660)
            throw new ArgumentException("Usa inicio y fin UTC con inicio < fin y un máximo de diez años.");
        return cuenta.Rol == Rol.Administrador;
    }

    public async Task<ReporteSolicitudesDto> SolicitudesAsync(Guid usuario, DateTime inicio, DateTime fin)
    {
        var admin = await PrepararAsync(usuario, inicio, fin);
        var grupos = await db.SolicitudesPermiso.AsNoTracking()
            .Where(s => s.CreadaEn >= inicio && s.CreadaEn < fin && (admin || s.SolicitanteId == usuario))
            .GroupBy(s => s.Estado).OrderBy(g => g.Key)
            .Select(g => new SolicitudesPorEstadoDto(g.Key, g.Count())).ToListAsync();
        return new(inicio, fin, grupos.Sum(g => g.Total), grupos);
    }

    public async Task<ReporteReservasDto> ReservasAsync(Guid usuario, DateTime inicio, DateTime fin)
    {
        var admin = await PrepararAsync(usuario, inicio, fin);
        var filas = db.Reservas.AsNoTracking()
            .Where(r => r.FechaInicio < fin && r.FechaFin > inicio && r.Estado != EstadoReserva.Cancelada
                && (admin || r.UsuarioId == usuario))
            .Select(r => new
            {
                r.EspacioId, Espacio = r.Espacio.Nombre, r.Estado,
                Inicio = r.FechaInicio > inicio ? r.FechaInicio : inicio,
                Fin = r.FechaFin < fin ? r.FechaFin : fin
            });
        List<ReservasPorEspacioDto> grupos;
        if (db.Database.IsRelational())
            grupos = await filas.GroupBy(r => new { r.EspacioId, r.Espacio, r.Estado })
                .Select(g => new ReservasPorEspacioDto(g.Key.EspacioId, g.Key.Espacio, g.Key.Estado,
                    g.Count(), g.Sum(r => (long)EF.Functions.DateDiffSecond(r.Inicio, r.Fin)) / 3600.0))
                .ToListAsync();
        else
            // EF InMemory no implementa DateDiffSecond. La alternativa solo sirve a las pruebas.
            grupos = await filas.GroupBy(r => new { r.EspacioId, r.Espacio, r.Estado })
                .Select(g => new ReservasPorEspacioDto(g.Key.EspacioId, g.Key.Espacio, g.Key.Estado,
                    g.Count(), g.Sum(r => (r.Fin - r.Inicio).TotalSeconds) / 3600.0)).ToListAsync();
        grupos = grupos.OrderByDescending(g => g.Horas).ThenByDescending(g => g.Reservas)
            .ThenBy(g => g.EspacioId).ThenBy(g => g.Estado).ToList();
        return new(inicio, fin, grupos.Sum(g => g.Reservas), grupos.Sum(g => g.Horas), grupos);
    }
}
