using SISReservas.Api.Models.Enums;

namespace SISReservas.Api.Models;

public class TokenAcceso
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid UsuarioId { get; set; }

    public Usuario Usuario { get; set; } = null!;

    public string TokenHash { get; set; } = string.Empty;

    public TipoToken Tipo { get; set; }

    public DateTime ExpiraEn { get; set; }

    public bool Usado { get; set; }

    public DateTime CreadoEn { get; set; } = DateTime.UtcNow;
}