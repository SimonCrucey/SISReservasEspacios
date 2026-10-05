using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SISReservas.Api.DTOs.Users;
using SISReservas.Api.Models.Enums;
using SISReservas.Api.Services;

[ApiController]
[Route("api/users")]
[Authorize(Roles = nameof(Rol.Administrador))]
public class UsersController : ControllerBase
{
    private readonly UserAdministrationService _userAdministrationService;

    public UsersController(
        UserAdministrationService userAdministrationService)
    {
        _userAdministrationService = userAdministrationService;
    }

    [HttpGet]
    public async Task<ActionResult<List<UserResponseDto>>> GetUsers()
    {
        var users = await _userAdministrationService.GetUsersAsync();

        return Ok(users);
    }

    [HttpPut("{id:guid}/role")]
    public async Task<IActionResult> ChangeRole(
        Guid id,
        [FromBody] ChangeUserRoleDto request)
    {
        var currentUserId = GetCurrentUserId();

        if (currentUserId == id && request.Rol != Rol.Administrador)
            return BadRequest("No puedes quitarte tu propio rol de Administrador.");

        var result = await _userAdministrationService.ChangeRoleAsync(
            id,
            request.Rol);

        if (!result)
            return NotFound();

        return NoContent();
    }

    [HttpPost("{id:guid}/deactivate")]
    public async Task<IActionResult> Deactivate(Guid id)
    {
        var currentUserId = GetCurrentUserId();

        if (currentUserId == id)
            return BadRequest("No puedes desactivar tu propio usuario.");

        var result = await _userAdministrationService.DeactivateAsync(id);

        if (!result)
            return NotFound();

        return NoContent();
    }

    [HttpPost("{id:guid}/activate")]
    public async Task<IActionResult> Activate(Guid id)
    {
        var result = await _userAdministrationService.ActivateAsync(id);

        if (!result)
            return NotFound();

        return NoContent();
    }

    private Guid? GetCurrentUserId()
    {
        var claim = User.FindFirst(
            System.Security.Claims.ClaimTypes.NameIdentifier);

        if (claim is null)
            return null;

        return Guid.TryParse(claim.Value, out var id)
            ? id
            : null;
    }
}