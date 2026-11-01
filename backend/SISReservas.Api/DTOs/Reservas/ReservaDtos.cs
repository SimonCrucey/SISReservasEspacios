using SISReservas.Api.Models.Enums;

namespace SISReservas.Api.DTOs.Reservas;

public sealed class CrearReservaDto
{
    public int EspacioId { get; set; }
    public DateTime FechaInicio { get; set; }
    public DateTime FechaFin { get; set; }
}

public sealed record EspacioResponseDto(int Id, string Nombre, TipoEspacio Tipo);
public sealed record DisponibilidadResponseDto(int EspacioId, bool Disponible);
public sealed record ReservaResponseDto(
    int Id, int EspacioId, DateTime FechaInicio, DateTime FechaFin, EstadoReserva Estado);
