using System.Text.Json;
using SISReservas.Api.Serialization;

namespace SISReservas.Api.Tests;

public class UtcDateTimeJsonTests
{
    private static readonly JsonSerializerOptions Options = new()
    {
        Converters = { new UtcDateTimeJsonConverter() }
    };

    [Theory]
    [InlineData("2026-11-23T03:19:37.9150329")]
    [InlineData("2026-11-23T00:00:00")]
    [InlineData("2026-12-01T03:59:59.9999999")]
    public void FechaSql_SerializaUtc_SinDesplazarTicks(string sql)
    {
        // Arrange: el lector SQL devuelve DateTimeKind.Unspecified.
        var stored = DateTime.Parse(sql, System.Globalization.CultureInfo.InvariantCulture);
        Assert.Equal(DateTimeKind.Unspecified, stored.Kind);
        // Act
        var ambiguous = JsonSerializer.Serialize(stored);
        var json = JsonSerializer.Serialize(stored, Options);
        var result = JsonSerializer.Deserialize<DateTime>(json, Options);
        // Assert
        Assert.DoesNotContain("Z", ambiguous);
        Assert.EndsWith("Z\"", json);
        Assert.Equal(stored.Ticks, result.Ticks);
        Assert.Equal(DateTimeKind.Utc, result.Kind);
    }

    [Fact]
    public void UtcYLocal_ConservanElMismoInstante()
    {
        // Arrange
        var utc = new DateTime(2026, 11, 23, 3, 19, 37, DateTimeKind.Utc);
        // Act
        var direct = JsonSerializer.Serialize(utc, Options);
        var local = JsonSerializer.Serialize(utc.ToLocalTime(), Options);
        // Assert
        Assert.Equal("\"2026-11-23T03:19:37Z\"", direct);
        Assert.Equal(direct, local);
    }

    [Fact]
    public void FechasNullableYDateTimeOffset_ConservanZonaYNulos()
    {
        // Arrange
        var source = new { EnviadoEn = (DateTime?)new DateTime(2026, 11, 23, 3, 19, 37),
            LeidaEn = (DateTime?)null, SubidoEn = new DateTimeOffset(2026, 11, 22, 23, 19, 37, TimeSpan.FromHours(-4)),
            Dia = new DateOnly(2026, 11, 22) };
        // Act
        using var json = JsonDocument.Parse(JsonSerializer.Serialize(source, Options));
        // Assert
        Assert.Equal("2026-11-23T03:19:37Z", json.RootElement.GetProperty("EnviadoEn").GetString());
        Assert.Equal(JsonValueKind.Null, json.RootElement.GetProperty("LeidaEn").ValueKind);
        Assert.EndsWith("-04:00", json.RootElement.GetProperty("SubidoEn").GetString());
        Assert.Equal("2026-11-22", json.RootElement.GetProperty("Dia").GetString());
    }

    [Fact]
    public void EntradaSinZona_NoSeConvierteImplicitamenteAUtc()
    {
        // Arrange: preservar el rechazo de fechas sin zona en los servicios.
        const string input = "\"2026-11-22T23:19:37\"";
        // Act
        var date = JsonSerializer.Deserialize<DateTime>(input, Options);
        // Assert
        Assert.Equal(DateTimeKind.Unspecified, date.Kind);
        Assert.Equal(23, date.Hour);
    }

    [Theory]
    [InlineData("123")]
    [InlineData("null")]
    [InlineData("\"no-es-fecha\"")]
    public void EntradaInvalida_ConservaErrorJsonControlado(string input)
    {
        // Arrange / Act
        var error = Record.Exception(() => JsonSerializer.Deserialize<DateTime>(input, Options));
        // Assert
        Assert.IsType<JsonException>(error);
    }
}
