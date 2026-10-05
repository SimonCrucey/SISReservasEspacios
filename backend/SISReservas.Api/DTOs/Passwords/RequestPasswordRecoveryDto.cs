using System.ComponentModel.DataAnnotations;

namespace SISReservas.Api.DTOs.Passwords;

public class RequestPasswordRecoveryDto
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
}