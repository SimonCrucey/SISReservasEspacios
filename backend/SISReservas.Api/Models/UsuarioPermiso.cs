using SISReservas.Api.Models.Enums;

namespace SISReservas.Api.Models;

public class UsuarioPermiso
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid UsuarioId { get; set; }

    public Usuario Usuario { get; set; } = null!;

    public Permiso Permiso { get; set; }

    public DateTime OtorgadoEn { get; set; } = DateTime.UtcNow;
}
