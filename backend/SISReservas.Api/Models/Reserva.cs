using SISReservas.Api.Models.Enums;

namespace SISReservas.Api.Models;

public class Reserva
{
    public int Id { get; set; }

    public Guid UsuarioId { get; set; }

    public Usuario Usuario { get; set; } = null!;

    public int EspacioId { get; set; }

    public Espacio Espacio { get; set; } = null!;

    public DateTime FechaInicio { get; set; }

    public DateTime FechaFin { get; set; }

    public EstadoReserva Estado { get; set; } = EstadoReserva.Pendiente;
}
