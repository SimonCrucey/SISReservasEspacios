namespace SISReservas.Api.Models;

public class Sesion
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid UsuarioId { get; set; }

    public Usuario Usuario { get; set; } = null!;

    public string TokenHash { get; set; } = string.Empty;

    public DateTime CreadaEn { get; set; } = DateTime.UtcNow;

    public DateTime? RevocadaEn { get; set; }
}
