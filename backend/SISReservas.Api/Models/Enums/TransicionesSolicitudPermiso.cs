namespace SISReservas.Api.Models.Enums;

public static class TransicionesSolicitudPermiso
{
    public static readonly IReadOnlyDictionary<
        EstadoSolicitudPermiso,
        EstadoSolicitudPermiso[]> Permitidas =
        new Dictionary<EstadoSolicitudPermiso, EstadoSolicitudPermiso[]>
        {
            [EstadoSolicitudPermiso.Pendiente] =
            [
                EstadoSolicitudPermiso.Aprobada,
                EstadoSolicitudPermiso.Rechazada
            ],

            [EstadoSolicitudPermiso.Aprobada] =
            [
                EstadoSolicitudPermiso.Aplicada
            ],

            [EstadoSolicitudPermiso.Rechazada] = [],

            [EstadoSolicitudPermiso.Aplicada] = [EstadoSolicitudPermiso.Rechazada]
        };

    public static bool EsPermitida(
        EstadoSolicitudPermiso desde,
        EstadoSolicitudPermiso hacia)
    {
        return Permitidas.TryGetValue(desde, out var destinos)
            && destinos.Contains(hacia);
    }

    public static bool EsProhibida(
        EstadoSolicitudPermiso desde,
        EstadoSolicitudPermiso hacia)
    {
        return !EsPermitida(desde, hacia);
    }

    public static void AsegurarPermitida(
        EstadoSolicitudPermiso desde,
        EstadoSolicitudPermiso hacia)
    {
        if (!EsPermitida(desde, hacia))
        {
            throw new InvalidOperationException(
                $"Transición no permitida: {desde} → {hacia}.");
        }
    }
}
