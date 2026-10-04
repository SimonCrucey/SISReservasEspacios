namespace SISReservas.Api.DTOs.Auth;

public class AuthResponse
{
    public string Message { get; set; } = string.Empty;

    public string? SessionToken { get; set; }
}