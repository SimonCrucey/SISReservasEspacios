using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SISReservas.Api.DTOs.Reservas;
using SISReservas.Api.Models.Enums;
using SISReservas.Api.Services;

namespace SISReservas.Api.Controllers;

[ApiController]
[Route("api/reservas")]
[Authorize(AuthenticationSchemes = "Session", Roles = nameof(Rol.Estandar) + "," + nameof(Rol.Administrador))]
public class ReservasController(ReservaService reservas) : ControllerBase
{
    [HttpGet("espacios")]
    public Task<IActionResult> ListarEspacios() => Ejecutar(reservas.ListarEspaciosAsync);

    [HttpGet("espacios/{espacioId:int}/disponibilidad")]
    public Task<IActionResult> Disponibilidad(int espacioId, [FromQuery] DateTime inicio, [FromQuery] DateTime fin) =>
        Ejecutar(id => reservas.ConsultarDisponibilidadAsync(id, espacioId, inicio, fin));

    [HttpPost]
    public Task<IActionResult> Crear([FromBody] CrearReservaDto request) =>
        Ejecutar(id => reservas.CrearAsync(id, request), StatusCodes.Status201Created);

    [HttpGet("mine")]
    public Task<IActionResult> ObtenerMias() => Ejecutar(reservas.ObtenerMiasAsync);

    [Authorize(Roles = nameof(Rol.Administrador))]
    [HttpPost("{id:int}/confirmar")]
    public Task<IActionResult> Confirmar(int id) => Ejecutar(usuario => reservas.ConfirmarAsync(usuario, id));

    [HttpPost("{id:int}/cancelar")]
    public Task<IActionResult> Cancelar(int id) => Ejecutar(usuario => reservas.CancelarAsync(usuario, id));

    private async Task<IActionResult> Ejecutar<T>(Func<Guid, Task<ResultadoReserva<T>>> operacion, int exito = 200)
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id)) return Unauthorized();
        var resultado = await operacion(id);
        if (resultado.Exito) return StatusCode(exito, resultado.Valor);
        var codigo = resultado.Motivo switch
        {
            MotivoResultadoReserva.DatosInvalidos => 400,
            MotivoResultadoReserva.NoAutorizado => 401,
            MotivoResultadoReserva.Prohibido => 403,
            MotivoResultadoReserva.NoEncontrado => 404,
            MotivoResultadoReserva.Conflicto => 409,
            _ => 503
        };
        return StatusCode(codigo, new { message = resultado.Mensaje });
    }
}
