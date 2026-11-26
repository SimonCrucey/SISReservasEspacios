using SISReservas.Api.Models.Enums;

namespace SISReservas.Api.DTOs.Reportes;

public record ReservasPorEspacioDto(int EspacioId, string Espacio, EstadoReserva Estado, int Reservas, double Horas);
public record ReporteReservasDto(DateTime Inicio, DateTime Fin, int TotalReservas, double TotalHoras, List<ReservasPorEspacioDto> Grupos);
