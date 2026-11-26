using Microsoft.EntityFrameworkCore;
using SISReservas.Api.DTOs.Reportes;
using SISReservas.Api.Models.Enums;

namespace SISReservas.Api.Services;

public partial class ReporteService
{
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
            // DATEDIFF cuenta límites. Ajustar las fracciones recupera ticks de datetime2(7).
            // SUM(decimal) evita desbordar bigint al acumular muchos intervalos.
            grupos = await filas.GroupBy(r => new { r.EspacioId, r.Espacio, r.Estado })
                .Select(g => new ReservasPorEspacioDto(g.Key.EspacioId, g.Key.Espacio, g.Key.Estado,
                    g.Count(), (double)g.Sum(r => (decimal)(
                        (long)EF.Functions.DateDiffSecond(r.Inicio, r.Fin) * TimeSpan.TicksPerSecond
                        + (r.Fin.Millisecond - r.Inicio.Millisecond) * TimeSpan.TicksPerMillisecond
                        + (r.Fin.Microsecond - r.Inicio.Microsecond) * 10L
                        + (r.Fin.Nanosecond - r.Inicio.Nanosecond) / 100)) / TimeSpan.TicksPerHour))
                .ToListAsync();
        else
            // InMemory calcula los mismos ticks sin las funciones exclusivas de SQL Server.
            grupos = await filas.GroupBy(r => new { r.EspacioId, r.Espacio, r.Estado })
                .Select(g => new ReservasPorEspacioDto(g.Key.EspacioId, g.Key.Espacio, g.Key.Estado,
                    g.Count(), (double)g.Sum(r => (decimal)(r.Fin.Ticks - r.Inicio.Ticks))
                        / TimeSpan.TicksPerHour)).ToListAsync();
        grupos = grupos.OrderByDescending(g => g.Horas).ThenByDescending(g => g.Reservas)
            .ThenBy(g => g.EspacioId).ThenBy(g => g.Estado).ToList();
        return new(inicio, fin, grupos.Sum(g => g.Reservas), grupos.Sum(g => g.Horas), grupos);
    }
}
