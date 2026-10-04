using Microsoft.AspNetCore.Mvc;
using SISReservas.Api.DTOs.Auth;
using SISReservas.Api.Services;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace SISReservas.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly AuthService _authService;
    private readonly SessionService _sessionService;

    public AuthController(
    AuthService authService,
    SessionService sessionService)
    {
        _authService = authService;
        _sessionService = sessionService;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register(
        RegisterRequest request)
    {
        try
        {
            var response = await _authService.RegisterAsync(request);

            return Ok(response);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new
            {
                message = ex.Message
            });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new
            {
                message = ex.Message
            });
        }
    }

    [HttpGet("activate")]
    public async Task<IActionResult> Activate(
        [FromQuery] string email,
        [FromQuery] string token)
    {
        try
        {
            var response =
                await _authService.ActivateAsync(
                    email,
                    token);

            return Ok(response);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new
            {
                message = ex.Message
            });
        }
    }

    [HttpPost("resend-activation")]
    public async Task<IActionResult> ResendActivation(
        [FromBody] string email)
    {
        try
        {
            var response =
                await _authService.ResendActivationAsync(email);

            return Ok(response);
        }
        catch
        {
            return Ok(new AuthResponse
            {
                Message = "Si la cuenta puede recibir una activación, se ha enviado un nuevo enlace."
            });
        }
    }
    [HttpPost("login")]
    public async Task<IActionResult> Login(
    LoginRequest request)
    {
        var response = await _authService.LoginAsync(
            request.Email,
            request.Password);

        if (response.Message == "Credenciales inválidas.")
        {
            return Unauthorized(response);
        }

        return Ok(response);
    }

    [Authorize]
    [HttpGet("me")]
    public IActionResult Me()
    {
        return Ok(new
        {
            id = User.FindFirstValue(ClaimTypes.NameIdentifier),
            nombre = User.FindFirstValue(ClaimTypes.Name),
            email = User.FindFirstValue(ClaimTypes.Email),
            rol = User.FindFirstValue(ClaimTypes.Role)
        });
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        if (!Request.Headers.TryGetValue("Authorization", out var authorization))
        {
            return Unauthorized();
        }

        var value = authorization.ToString();

        if (!value.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            return Unauthorized();
        }

        var token = value["Bearer ".Length..].Trim();

        await _sessionService.RevokeAsync(token);

        return Ok(new
        {
            message = "Sesión cerrada correctamente."
        });
    }
}