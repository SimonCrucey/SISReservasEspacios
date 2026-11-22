using SISReservas.Api.Models.Enums;

namespace SISReservas.Api.DTOs.Reportes;

public record SolicitudesPorEstadoDto(EstadoSolicitudPermiso Estado, int Total);
public record ReporteSolicitudesDto(DateTime Inicio, DateTime Fin, int Total, List<SolicitudesPorEstadoDto> Grupos);
public record ReservasPorEspacioDto(int EspacioId, string Espacio, EstadoReserva Estado, int Reservas, double Horas);
public record ReporteReservasDto(DateTime Inicio, DateTime Fin, int TotalReservas, double TotalHoras, List<ReservasPorEspacioDto> Grupos);
