using System.ComponentModel.DataAnnotations;

namespace SISReservas.Api.DTOs.Passwords;

public class ResetPasswordDto
{
    [Required]
    public string Token { get; set; } = string.Empty;

    [Required]
    [MinLength(8)]
    public string NuevaPassword { get; set; } = string.Empty;
}