using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SISReservas.Api.Services;

namespace SISReservas.Api.Controllers;

[ApiController]
[Route("api/reportes")]
[Authorize(AuthenticationSchemes = "Session", Roles = "Administrador,Estandar")]
public partial class ReportesController : ControllerBase
{
    private readonly ReporteService servicio;

    public ReportesController(ReporteService servicio) => this.servicio = servicio;

    private Guid Usuario => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet("solicitudes")]
    public Task<IActionResult> Solicitudes([FromQuery] DateTime inicio, [FromQuery] DateTime fin) =>
        Ejecutar(async () => Ok(await servicio.SolicitudesAsync(Usuario, inicio, fin)));

    private async Task<IActionResult> Ejecutar(Func<Task<IActionResult>> accion)
    {
        try { return await accion(); }
        catch (ArgumentException error) { return BadRequest(new { message = error.Message }); }
        catch (UnauthorizedAccessException) { return Forbid(); }
    }
}
