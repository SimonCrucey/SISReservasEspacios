using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SISReservas.Api.DTOs.Notificaciones;
using SISReservas.Api.Services;

namespace SISReservas.Api.Controllers;

[ApiController]
[Route("api/notificaciones")]
[Authorize(AuthenticationSchemes = "Session", Roles = "Administrador,Estandar")]
public class NotificacionesController(NotificacionService servicio) : ControllerBase
{
    private Guid Usuario => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet]
    public Task<IActionResult> Bandeja() => Ejecutar(async () => Ok(await servicio.ConsultarAsync(Usuario)));

    [HttpPatch("{id:guid}/leida")]
    public Task<IActionResult> Leer(Guid id) => Ejecutar(async () =>
        await servicio.LeerAsync(Usuario, id) ? NoContent() : NotFound());

    [HttpGet("preferencias")]
    public Task<IActionResult> Preferencias() => Ejecutar(async () => Ok(await servicio.PreferenciasAsync(Usuario)));

    [HttpPut("preferencias")]
    public Task<IActionResult> CambiarPreferencias(PreferenciasDto request) => Ejecutar(async () =>
        Ok(await servicio.PreferenciasAsync(Usuario, request.CorreoHabilitado)));

    private async Task<IActionResult> Ejecutar(Func<Task<IActionResult>> accion)
    {
        try { return await accion(); }
        catch (UnauthorizedAccessException) { return Forbid(); }
    }
}
