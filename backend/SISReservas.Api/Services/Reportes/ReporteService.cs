using Microsoft.EntityFrameworkCore;
using SISReservas.Api.Data;
using SISReservas.Api.DTOs.Reportes;
using SISReservas.Api.Models.Enums;

namespace SISReservas.Api.Services;

public partial class ReporteService
{
    private readonly SISReservasDbContext db;

    public ReporteService(SISReservasDbContext db) => this.db = db;

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


}
