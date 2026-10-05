using SISReservas.Api.Models.Enums;

namespace SISReservas.Api.Models;

public class Reserva
{
    public int Id { get; set; }

    public int UsuarioId { get; set; }

    public DateTime FechaInicio { get; set; }

    public DateTime FechaFin { get; set; }

    public EstadoReserva Estado { get; set; } = EstadoReserva.Pendiente;
}