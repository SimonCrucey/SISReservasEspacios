using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SISReservas.Api.Data;
using SISReservas.Api.Models.Enums;

namespace SISReservas.Api.Controllers;

[ApiController]
[Route("api/correos")]
[Authorize(AuthenticationSchemes = "Session", Roles = "Administrador")]
public class CorreosController(SISReservasDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Consultar([FromQuery] int pagina = 1)
    {
        if (pagina < 1 || pagina > 100000) return BadRequest(new { message = "Página inválida." });
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id)) return Unauthorized();
        if (!await db.Usuarios.AnyAsync(u => u.Id == id && u.Activo && u.Rol == Rol.Administrador)) return Forbid();
        var total = await db.CorreosEnCola.CountAsync();
        var correos = await db.CorreosEnCola.AsNoTracking().OrderByDescending(c => c.CreadoEn).ThenBy(c => c.Id)
            .Skip((pagina - 1) * 100).Take(100)
            .Select(c => new { c.Id, c.Destinatario, c.Estado, c.Intentos, c.CreadoEn, c.EnviadoEn, c.UltimoIntentoEn, c.UltimoError })
            .ToListAsync();
        return Ok(new { total, pagina, correos });
    }
}
