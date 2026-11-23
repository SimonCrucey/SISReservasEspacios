using System.Text.Json;
using System.Text.Json.Serialization;

namespace SISReservas.Api.Serialization;

// Los DateTime del contrato representan instantes UTC. SQL datetime2 no guarda Kind.
// Esta convención de salida no desplaza los ticks recuperados de SQL ni cambia entradas.
public sealed class UtcDateTimeJsonConverter : JsonConverter<DateTime>
{
    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.String || !reader.TryGetDateTime(out var value))
            throw new JsonException("Usa una fecha y hora ISO 8601 válida.");
        return value;
    }

    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
        => writer.WriteStringValue(value.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(value, DateTimeKind.Utc)
            : value.ToUniversalTime());
}
