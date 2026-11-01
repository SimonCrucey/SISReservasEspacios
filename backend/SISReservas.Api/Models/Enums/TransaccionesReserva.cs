namespace SISReservas.Api.Models.Enums;

public static class TransicionesReserva
{
    public static readonly IReadOnlyDictionary<EstadoReserva, EstadoReserva[]> Permitidas =
        new Dictionary<EstadoReserva, EstadoReserva[]>
        {
            [EstadoReserva.Pendiente] =
            [
                EstadoReserva.Confirmada,
                EstadoReserva.Cancelada
            ],

            [EstadoReserva.Confirmada] =
            [
                EstadoReserva.Completada,
                EstadoReserva.Cancelada
            ],

            [EstadoReserva.Cancelada] = [EstadoReserva.Confirmada],

            [EstadoReserva.Completada] = []
        };

    public static bool EsPermitida(
        EstadoReserva desde,
        EstadoReserva hacia)
    {
        return Permitidas.TryGetValue(desde, out var destinos)
            && destinos.Contains(hacia);
    }

    public static bool EsProhibida(
        EstadoReserva desde,
        EstadoReserva hacia)
    {
        return !EsPermitida(desde, hacia);
    }

    public static void AsegurarPermitida(EstadoReserva desde, EstadoReserva hacia)
    {
        if (!EsPermitida(desde, hacia))
        {
            throw new InvalidOperationException(
                $"Transición no permitida: {desde} → {hacia}.");
        }
    }

    public static bool EsTerminal(EstadoReserva estado)
    {
        return Permitidas.TryGetValue(estado, out var destinos)
            && destinos.Length == 0;
    }
}
