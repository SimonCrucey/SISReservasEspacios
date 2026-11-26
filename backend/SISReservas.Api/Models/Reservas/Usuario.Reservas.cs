namespace SISReservas.Api.Models;

public partial class Usuario
{
    public ICollection<Reserva> Reservas { get; set; } = new List<Reserva>();
}
