using Microsoft.AspNetCore.Mvc;
using SISReservas.Api.DTOs.Auth;
using SISReservas.Api.Services;

namespace SISReservas.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly AuthService _authService;

    public AuthController(
        AuthService authService)
    {
        _authService = authService;
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
}