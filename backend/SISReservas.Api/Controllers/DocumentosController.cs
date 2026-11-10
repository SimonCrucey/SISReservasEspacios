using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SISReservas.Api.Models.Enums;
using SISReservas.Api.Services;

namespace SISReservas.Api.Controllers;

[ApiController]
[Route("api/documentos")]
[Authorize(AuthenticationSchemes = "Session", Roles = nameof(Rol.Estandar) + "," + nameof(Rol.Administrador))]
public class DocumentosController(DocumentoService documentos) : ControllerBase
{
    [HttpPost]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(DocumentoService.TamanoMaximo + 65536)]
    public Task<IActionResult> Subir(IFormFile? archivo, CancellationToken ct) => Ejecutar(async (id, _) =>
    {
        var documento = await documentos.SubirAsync(id, archivo, ct);
        if (documento is null)
            return BadRequest(new { message = "Envía un archivo no vacío de hasta 10 MiB con un nombre válido." });

        return CreatedAtAction(nameof(Descargar), new { id = documento.Id }, documento);
    });

    [HttpGet]
    public Task<IActionResult> Listar(CancellationToken ct) =>
        Ejecutar(async (usuario, admin) => Ok(await documentos.ListarAsync(usuario, admin, ct)));

    [HttpGet("{id:guid}/descargar")]
    public Task<IActionResult> Descargar(Guid id, CancellationToken ct) => Ejecutar(async (usuario, admin) =>
    {
        var descarga = await documentos.DescargarAsync(usuario, admin, id, ct);
        if (descarga is not { } archivo) return NotFound();

        Response.Headers["X-Content-Type-Options"] = "nosniff";
        return File(archivo.Contenido, archivo.Mime, archivo.Nombre);
    });

    [HttpDelete("{id:guid}")]
    public Task<IActionResult> Eliminar(Guid id, CancellationToken ct) => Ejecutar(async (usuario, admin) =>
    {
        if (!await documentos.EliminarAsync(usuario, admin, id, ct)) return NotFound();
        return NoContent();
    });

    private async Task<IActionResult> Ejecutar(Func<Guid, bool, Task<IActionResult>> operacion)
    {
        if (User.Identity?.IsAuthenticated != true ||
            !Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id))
            return Unauthorized();

        var administrador = User.IsInRole(nameof(Rol.Administrador));
        if (!administrador && !User.IsInRole(nameof(Rol.Estandar))) return Forbid();

        try
        {
            return await operacion(id, administrador);
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or DbUpdateException)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable,
                new { message = "El almacenamiento de documentos no está disponible." });
        }
    }
}
