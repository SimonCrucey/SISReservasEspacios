using SISReservas.Api.Models.Enums;

namespace SISReservas.Api.Tests;

public class TransicionesSolicitudPermisoTests
{
    public static TheoryData<
        EstadoSolicitudPermiso,
        EstadoSolicitudPermiso,
        bool> Casos => new()
    {
        {
            EstadoSolicitudPermiso.Pendiente,
            EstadoSolicitudPermiso.Pendiente,
            false
        },
        {
            EstadoSolicitudPermiso.Pendiente,
            EstadoSolicitudPermiso.Aprobada,
            true
        },
        {
            EstadoSolicitudPermiso.Pendiente,
            EstadoSolicitudPermiso.Rechazada,
            true
        },
        {
            EstadoSolicitudPermiso.Pendiente,
            EstadoSolicitudPermiso.Aplicada,
            false
        },
        {
            EstadoSolicitudPermiso.Aprobada,
            EstadoSolicitudPermiso.Pendiente,
            false
        },
        {
            EstadoSolicitudPermiso.Aprobada,
            EstadoSolicitudPermiso.Aprobada,
            false
        },
        {
            EstadoSolicitudPermiso.Aprobada,
            EstadoSolicitudPermiso.Rechazada,
            false
        },
        {
            EstadoSolicitudPermiso.Aprobada,
            EstadoSolicitudPermiso.Aplicada,
            true
        },
        {
            EstadoSolicitudPermiso.Rechazada,
            EstadoSolicitudPermiso.Pendiente,
            false
        },
        {
            EstadoSolicitudPermiso.Rechazada,
            EstadoSolicitudPermiso.Aprobada,
            false
        },
        {
            EstadoSolicitudPermiso.Rechazada,
            EstadoSolicitudPermiso.Rechazada,
            false
        },
        {
            EstadoSolicitudPermiso.Rechazada,
            EstadoSolicitudPermiso.Aplicada,
            false
        },
        {
            EstadoSolicitudPermiso.Aplicada,
            EstadoSolicitudPermiso.Pendiente,
            false
        },
        {
            EstadoSolicitudPermiso.Aplicada,
            EstadoSolicitudPermiso.Aprobada,
            false
        },
        {
            EstadoSolicitudPermiso.Aplicada,
            EstadoSolicitudPermiso.Rechazada,
            false
        },
        {
            EstadoSolicitudPermiso.Aplicada,
            EstadoSolicitudPermiso.Aplicada,
            false
        }
    };

    [Theory]
    [MemberData(nameof(Casos))]
    public void EsPermitida_RespetaLaMaquinaDeEstados(
        EstadoSolicitudPermiso desde,
        EstadoSolicitudPermiso hacia,
        bool esperada)
    {
        Assert.Equal(
            esperada,
            TransicionesSolicitudPermiso.EsPermitida(
                desde,
                hacia));

        Assert.Equal(
            !esperada,
            TransicionesSolicitudPermiso.EsProhibida(
                desde,
                hacia));

        if (esperada)
        {
            var excepcion = Record.Exception(() =>
                TransicionesSolicitudPermiso.AsegurarPermitida(
                    desde,
                    hacia));

            Assert.Null(excepcion);
        }
        else
        {
            Assert.Throws<InvalidOperationException>(() =>
                TransicionesSolicitudPermiso.AsegurarPermitida(
                    desde,
                    hacia));
        }
    }
}
