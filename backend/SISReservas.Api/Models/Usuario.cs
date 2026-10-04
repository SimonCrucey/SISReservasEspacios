using SISReservas.Api.Models.Enums;

namespace SISReservas.Api.Models;

public class Usuario
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Nombre { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public Rol Rol { get; set; } = Rol.Estandar;

    public bool Activo { get; set; }

    public int IntentosFallidos { get; set; }

    public DateTime? BloqueadoHasta { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<TokenAcceso> TokensAcceso { get; set; } =
        new List<TokenAcceso>();

    public ICollection<CorreoEnCola> Correos { get; set; } =
        new List<CorreoEnCola>();
}