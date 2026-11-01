using SISReservas.Api.Models.Enums;

namespace SISReservas.Api.Models;

public class Espacio
{
    public int Id { get; set; }

    public string Nombre { get; set; } = string.Empty;

    public TipoEspacio Tipo { get; set; }

    public bool Activo { get; set; } = true;

    public ICollection<Reserva> Reservas { get; set; } = new List<Reserva>();
}
