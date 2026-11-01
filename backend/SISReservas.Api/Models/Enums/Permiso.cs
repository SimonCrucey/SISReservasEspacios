namespace SISReservas.Api.Models.Enums;

public enum Permiso
{
    GestionarUsuarios = 1,
    RestablecerContrasenas = 2,
    ConsultarUsuarios = 3,
    ReservarEspacios = 4
}

public static class PermisosSolicitables
{
    public static readonly IReadOnlySet<Permiso> Valores =
        new HashSet<Permiso>
        {
            Permiso.ConsultarUsuarios,
            Permiso.ReservarEspacios
        };

    public static bool Contiene(Permiso permiso)
    {
        return Valores.Contains(permiso);
    }
}
