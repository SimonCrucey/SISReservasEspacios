using SISReservas.Api.Models;
using SISReservas.Api.Models.Enums;

namespace SISReservas.Api.Tests;

public class TransicionesReservaTests
{
    [Theory]
    [InlineData(EstadoReserva.Pendiente, EstadoReserva.Confirmada)]
    [InlineData(EstadoReserva.Pendiente, EstadoReserva.Cancelada)]
    [InlineData(EstadoReserva.Confirmada, EstadoReserva.Completada)]
    [InlineData(EstadoReserva.Confirmada, EstadoReserva.Cancelada)]
    public void AsegurarPermitida_TransicionDocumentada_NoRechaza(
        EstadoReserva desde, EstadoReserva hacia)
    {
        // Arrange
        var reserva = new Reserva { Estado = desde };

        // Act
        var excepcion = Record.Exception(() =>
            TransicionesReserva.AsegurarPermitida(reserva.Estado, hacia));

        // Assert
        Assert.Null(excepcion);
        Assert.True(TransicionesReserva.EsPermitida(desde, hacia));
        Assert.False(TransicionesReserva.EsProhibida(desde, hacia));
    }

    [Theory]
    [InlineData(EstadoReserva.Cancelada, EstadoReserva.Confirmada)]
    [InlineData(EstadoReserva.Cancelada, EstadoReserva.Completada)]
    [InlineData(EstadoReserva.Completada, EstadoReserva.Cancelada)]
    [InlineData(EstadoReserva.Completada, EstadoReserva.Confirmada)]
    public void AsegurarPermitida_ProhibidaDocumentada_RechazaSinCambiarReserva(
        EstadoReserva desde, EstadoReserva hacia)
    {
        // Arrange
        var reserva = new Reserva { Estado = desde };

        // Act
        var excepcion = Record.Exception(() =>
        {
            TransicionesReserva.AsegurarPermitida(reserva.Estado, hacia);
            reserva.Estado = hacia;
        });

        // Assert
        Assert.IsType<InvalidOperationException>(excepcion);
        Assert.Equal(desde, reserva.Estado);
        Assert.True(TransicionesReserva.EsProhibida(desde, hacia));
        Assert.False(TransicionesReserva.EsPermitida(desde, hacia));
    }

    public static IEnumerable<object[]> SalidasTerminales =>
        new[] { EstadoReserva.Cancelada, EstadoReserva.Completada }
            .SelectMany(desde => Enum.GetValues<EstadoReserva>()
                .Select(hacia => new object[] { desde, hacia }));

    [Theory]
    [MemberData(nameof(SalidasTerminales))]
    public void AsegurarPermitida_DesdeTerminal_RechazaCualquierDestino(
        EstadoReserva desde, EstadoReserva hacia)
    {
        // Arrange
        var reserva = new Reserva { Estado = desde };

        // Act
        var excepcion = Record.Exception(() =>
        {
            TransicionesReserva.AsegurarPermitida(reserva.Estado, hacia);
            reserva.Estado = hacia;
        });

        // Assert
        Assert.IsType<InvalidOperationException>(excepcion);
        Assert.Equal(desde, reserva.Estado);
    }

    [Theory]
    [InlineData(EstadoReserva.Pendiente, false)]
    [InlineData(EstadoReserva.Confirmada, false)]
    [InlineData(EstadoReserva.Cancelada, true)]
    [InlineData(EstadoReserva.Completada, true)]
    [InlineData((EstadoReserva)0, false)]
    [InlineData((EstadoReserva)999, false)]
    public void EsTerminal_ClasificaEstadoSinConsiderarTerminalUnValorInvalido(
        EstadoReserva estado, bool esperado)
    {
        // Arrange
        var estadoConsultado = estado;

        // Act
        var resultado = TransicionesReserva.EsTerminal(estadoConsultado);

        // Assert
        Assert.Equal(esperado, resultado);
    }

    [Theory]
    [InlineData((EstadoReserva)0, EstadoReserva.Confirmada)]
    [InlineData((EstadoReserva)999, EstadoReserva.Cancelada)]
    [InlineData(EstadoReserva.Pendiente, (EstadoReserva)0)]
    [InlineData(EstadoReserva.Confirmada, (EstadoReserva)999)]
    [InlineData((EstadoReserva)(-1), (EstadoReserva)999)]
    public void AsegurarPermitida_EnumInvalido_RechazaSinCambiarReserva(
        EstadoReserva desde, EstadoReserva hacia)
    {
        // Arrange
        var reserva = new Reserva { Estado = desde };

        // Act
        var excepcion = Record.Exception(() =>
        {
            TransicionesReserva.AsegurarPermitida(reserva.Estado, hacia);
            reserva.Estado = hacia;
        });

        // Assert
        Assert.IsType<InvalidOperationException>(excepcion);
        Assert.Equal(desde, reserva.Estado);
        Assert.False(TransicionesReserva.EsPermitida(desde, hacia));
        Assert.True(TransicionesReserva.EsProhibida(desde, hacia));
    }
}
