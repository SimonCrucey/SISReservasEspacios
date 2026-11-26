using SISReservas.Api.Models.Enums;

namespace SISReservas.Api.DTOs.Reportes;

public record SolicitudesPorEstadoDto(EstadoSolicitudPermiso Estado, int Total);
public record ReporteSolicitudesDto(DateTime Inicio, DateTime Fin, int Total, List<SolicitudesPorEstadoDto> Grupos);
