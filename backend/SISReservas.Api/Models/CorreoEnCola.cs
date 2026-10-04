using SISReservas.Api.Models.Enums;

namespace SISReservas.Api.Models;

public class CorreoEnCola
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid UsuarioId { get; set; }

    public Usuario Usuario { get; set; } = null!;

    public string Destinatario { get; set; } = string.Empty;

    public string Asunto { get; set; } = string.Empty;

    public string Cuerpo { get; set; } = string.Empty;

    public EstadoCorreo Estado { get; set; } = EstadoCorreo.Pendiente;

    public DateTime CreadoEn { get; set; } = DateTime.UtcNow;

    public DateTime? EnviadoEn { get; set; }
}