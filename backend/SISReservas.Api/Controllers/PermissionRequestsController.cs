using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SISReservas.Api.DTOs.Permisos;
using SISReservas.Api.Models.Enums;
using SISReservas.Api.Services;

namespace SISReservas.Api.Controllers;

[ApiController]
[Route("api/permission-requests")]
[Authorize]
public class PermissionRequestsController : ControllerBase
{
    private readonly SolicitudPermisoService _solicitudPermisoService;

    public PermissionRequestsController(
        SolicitudPermisoService solicitudPermisoService)
    {
        _solicitudPermisoService = solicitudPermisoService;
    }

    [Authorize(Roles = nameof(Rol.Estandar))]
    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CrearSolicitudPermisoDto request)
    {
        var currentUserId = GetCurrentUserId();

        if (currentUserId is null)
            return Unauthorized();

        var result = await _solicitudPermisoService.CrearAsync(
            currentUserId.Value,
            request);

        if (!result.Exito)
            return MapError(result);

        return StatusCode(
            StatusCodes.Status201Created,
            result.Valor);
    }

    [HttpGet("mine")]
    public async Task<IActionResult> GetMine()
    {
        var currentUserId = GetCurrentUserId();

        if (currentUserId is null)
            return Unauthorized();

        var result = await _solicitudPermisoService.ObtenerMiasAsync(
            currentUserId.Value);

        if (!result.Exito)
            return MapError(result);

        return Ok(result.Valor);
    }

    [Authorize(Roles = nameof(Rol.Administrador))]
    [HttpGet("pending")]
    public async Task<IActionResult> GetPending()
    {
        var currentUserId = GetCurrentUserId();

        if (currentUserId is null)
            return Unauthorized();

        var result = await _solicitudPermisoService.ObtenerPendientesAsync(
            currentUserId.Value);

        if (!result.Exito)
            return MapError(result);

        return Ok(result.Valor);
    }

    [Authorize(Roles = nameof(Rol.Administrador))]
    [HttpPost("{id:guid}/approve")]
    public async Task<IActionResult> Approve(
        Guid id)
    {
        var currentUserId = GetCurrentUserId();

        if (currentUserId is null)
            return Unauthorized();

        var result = await _solicitudPermisoService.AprobarAsync(
            id,
            currentUserId.Value);

        if (!result.Exito)
            return MapError(result);

        return Ok(result.Valor);
    }

    [Authorize(Roles = nameof(Rol.Administrador))]
    [HttpPost("{id:guid}/reject")]
    public async Task<IActionResult> Reject(
        Guid id,
        [FromBody] RechazarSolicitudPermisoDto request)
    {
        var currentUserId = GetCurrentUserId();

        if (currentUserId is null)
            return Unauthorized();

        var result = await _solicitudPermisoService.RechazarAsync(
            id,
            currentUserId.Value,
            request);

        if (!result.Exito)
            return MapError(result);

        return Ok(result.Valor);
    }

    private Guid? GetCurrentUserId()
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);

        return Guid.TryParse(claim, out var id)
            ? id
            : null;
    }

    private IActionResult MapError<T>(
        ResultadoSolicitudPermiso<T> result)
    {
        var body = new
        {
            message = result.Mensaje
        };

        return result.Motivo switch
        {
            MotivoResultadoSolicitudPermiso.DatosInvalidos or
            MotivoResultadoSolicitudPermiso.PermisoInexistente or
            MotivoResultadoSolicitudPermiso.PermisoNoSolicitable =>
                BadRequest(body),

            MotivoResultadoSolicitudPermiso.UsuarioNoEncontrado or
            MotivoResultadoSolicitudPermiso.SolicitudNoEncontrada =>
                NotFound(body),

            MotivoResultadoSolicitudPermiso.RolNoPermitido or
            MotivoResultadoSolicitudPermiso.ResolucionPropia =>
                StatusCode(
                    StatusCodes.Status403Forbidden,
                    body),

            MotivoResultadoSolicitudPermiso.EstadoInvalido or
            MotivoResultadoSolicitudPermiso.ConcesionDuplicada or
            MotivoResultadoSolicitudPermiso.ConflictoConcurrencia or
            MotivoResultadoSolicitudPermiso.UsuarioInactivo =>
                Conflict(body),

            _ => StatusCode(
                StatusCodes.Status500InternalServerError,
                body)
        };
    }
}
