using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SISReservas.Api.Data;
using SISReservas.Api.Models.Enums;

namespace SISReservas.Api.Services;

public record ClimaOptions(string Clave, string Ubicacion);
public record RecomendacionCancha(int EspacioId, bool Disponible, bool Recomendado, string Motivo);

public class PlanificacionService(HttpClient http, ClimaOptions options, ReservaService reservas, SISReservasDbContext db)
{
    public async Task<ResultadoReserva<RecomendacionCancha>> EvaluarAsync(
        Guid usuario, int espacioId, DateTime inicio, DateTime fin, CancellationToken ct)
    {
        var disponibilidad = await reservas.ConsultarDisponibilidadAsync(usuario, espacioId, inicio, fin);
        if (!disponibilidad.Exito)
            return ResultadoReserva<RecomendacionCancha>.Falla(disponibilidad.Motivo, disponibilidad.Mensaje);
        if (!await db.Espacios.AnyAsync(e => e.Id == espacioId && e.Tipo == TipoEspacio.Cancha, ct))
            return ResultadoReserva<RecomendacionCancha>.Falla(MotivoResultadoReserva.DatosInvalidos, "Selecciona una cancha para planificación exterior.");
        if (!disponibilidad.Valor!.Disponible)
            return ResultadoReserva<RecomendacionCancha>.Ok(new(espacioId, false, false, "Horario ocupado."));
        if (string.IsNullOrWhiteSpace(options.Clave))
            return Fallo("Configura WEATHER_API_KEY para consultar el pronóstico.");

        try
        {
            using var respuesta = await http.GetAsync(
                $"forecast.json?key={Uri.EscapeDataString(options.Clave)}&q={Uri.EscapeDataString(options.Ubicacion)}&days=3", ct);
            if (!respuesta.IsSuccessStatusCode) return Fallo("El proveedor meteorológico rechazó la consulta. Revisa la clave y la cuota.");
            using var json = JsonDocument.Parse(await respuesta.Content.ReadAsStringAsync(ct));
            var horas = json.RootElement.GetProperty("forecast").GetProperty("forecastday")
                .EnumerateArray().SelectMany(d => d.GetProperty("hour").EnumerateArray())
                .Select(h => new
                {
                    Fecha = DateTimeOffset.FromUnixTimeSeconds(h.GetProperty("time_epoch").GetInt64()).UtcDateTime,
                    Lluvia = h.GetProperty("chance_of_rain").GetDouble(),
                    Viento = h.GetProperty("wind_kph").GetDouble()
                }).OrderBy(h => h.Fecha).ToList();
            var seleccion = horas.Where(h => h.Fecha < fin && h.Fecha.AddHours(1) > inicio).ToList();
            if (seleccion.Count == 0 || seleccion[0].Fecha > inicio || seleccion[^1].Fecha.AddHours(1) < fin
                || seleccion.Zip(seleccion.Skip(1)).Any(p => p.Second.Fecha != p.First.Fecha.AddHours(1)))
                return Fallo("El intervalo queda fuera de la cobertura horaria del pronóstico.");
            var recomendable = seleccion.All(h => h.Lluvia < 60 && h.Viento < 40);
            return ResultadoReserva<RecomendacionCancha>.Ok(new(espacioId, true, recomendable,
                recomendable ? "Cancha disponible y condiciones favorables." : "Cancha disponible, pero conviene otro horario por lluvia o viento."));
        }
        catch (Exception error) when (error is HttpRequestException or JsonException or KeyNotFoundException
            or InvalidOperationException or FormatException or ArgumentOutOfRangeException)
        {
            return Fallo("No se pudo obtener un pronóstico válido. Intenta más tarde.");
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            return Fallo("El proveedor meteorológico tardó demasiado. Intenta más tarde.");
        }
    }

    private static ResultadoReserva<RecomendacionCancha> Fallo(string mensaje) =>
        ResultadoReserva<RecomendacionCancha>.Falla(MotivoResultadoReserva.ErrorPersistencia, mensaje);
}
