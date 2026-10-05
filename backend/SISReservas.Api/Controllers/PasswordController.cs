using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SISReservas.Api.DTOs.Passwords;
using SISReservas.Api.Models.Enums;
using SISReservas.Api.Services;

[ApiController]
[Route("api/passwords")]
public class PasswordsController : ControllerBase
{
    private readonly PasswordRecoveryService _passwordRecoveryService;

    public PasswordsController(
        PasswordRecoveryService passwordRecoveryService)
    {
        _passwordRecoveryService = passwordRecoveryService;
    }

    [AllowAnonymous]
    [HttpPost("recovery")]
    public async Task<IActionResult> RequestRecovery(
        [FromBody] RequestPasswordRecoveryDto request)
    {
        await _passwordRecoveryService.RequestRecoveryAsync(
            request.Email);

        return Ok(
            "Si el correo corresponde a una cuenta, " +
            "se ha iniciado el proceso de recuperación.");
    }

    [AllowAnonymous]
    [HttpPost("reset")]
    public async Task<IActionResult> ResetPassword(
        [FromBody] ResetPasswordDto request)
    {
        var result = await _passwordRecoveryService.ResetPasswordAsync(
            request.Token,
            request.NuevaPassword);

        if (!result)
        {
            return BadRequest(
                "El token no es válido, ha expirado o " +
                "la contraseña no cumple la política requerida.");
        }

        return Ok("Contraseña restablecida correctamente.");
    }

    [Authorize]
    [HttpPost("change")]
    public async Task<IActionResult> ChangePassword(
        [FromBody] ChangePasswordDto request)
    {
        var usuarioId = GetCurrentUserId();

        if (usuarioId is null)
        {
            return Unauthorized();
        }

        var result = await _passwordRecoveryService.ChangePasswordAsync(
            usuarioId.Value,
            request.PasswordActual,
            request.NuevaPassword);

        if (!result)
        {
            return BadRequest(
                "La contraseña actual es incorrecta o " +
                "la nueva contraseña no cumple la política requerida.");
        }

        return Ok("Contraseña cambiada correctamente.");
    }

    [Authorize(Roles = nameof(Rol.Administrador))]
    [HttpPost("{id:guid}/force-reset")]
    public async Task<IActionResult> ForcePasswordReset(Guid id)
    {
        var result = await _passwordRecoveryService
            .ForcePasswordResetAsync(id);

        if (!result)
        {
            return NotFound();
        }

        return Ok(
            "Se ha iniciado el proceso de restablecimiento " +
            "de contraseña para el usuario.");
    }

    private Guid? GetCurrentUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier);

        if (claim is null)
        {
            return null;
        }

        return Guid.TryParse(claim.Value, out var id)
            ? id
            : null;
    }
}