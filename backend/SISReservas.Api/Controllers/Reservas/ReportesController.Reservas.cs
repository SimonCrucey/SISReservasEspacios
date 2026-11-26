using Microsoft.AspNetCore.Mvc;

namespace SISReservas.Api.Controllers;

public partial class ReportesController
{
    [HttpGet("reservas")]
    public Task<IActionResult> Reservas([FromQuery] DateTime inicio, [FromQuery] DateTime fin) =>
        Ejecutar(async () => Ok(await servicio.ReservasAsync(Usuario, inicio, fin)));

}
