using System.ComponentModel.DataAnnotations;

namespace SISReservas.Api.DTOs.Passwords;

public class ChangePasswordDto
{
    [Required]
    public string PasswordActual { get; set; } = string.Empty;

    [Required]
    [MinLength(8)]
    public string NuevaPassword { get; set; } = string.Empty;
}