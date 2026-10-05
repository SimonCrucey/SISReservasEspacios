using SISReservas.Api.Models.Enums;

namespace SISReservas.Api.DTOs.Users;

public class UserResponseDto
{
    public Guid Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public SISReservas.Api.Models.Enums.Rol Rol { get; set; }
    public bool Activo { get; set; }
}