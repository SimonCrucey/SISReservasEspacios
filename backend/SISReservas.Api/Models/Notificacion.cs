namespace SISReservas.Api.Models;

public class Notificacion
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UsuarioId { get; set; }
    public required string Asunto { get; set; }
    public required string Mensaje { get; set; }
    public DateTime CreadaEn { get; set; } = DateTime.UtcNow;
    public DateTime? LeidaEn { get; set; }
}
