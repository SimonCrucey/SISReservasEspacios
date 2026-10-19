using Microsoft.EntityFrameworkCore;
using SISReservas.Api.Models;
using SISReservas.Api.Models.Enums;
using SISReservas.Api.Services;

namespace SISReservas.Api.Tests;

public class PermisoServiceTests
{
    [Fact]
    public async Task TienePermisoAsync_AdministradorActivo_ConcedeTodosLosPermisos()
    {
        using var database = new TestDatabase();
        using var db = database.CreateContext();

        var administrador = await TestData.AgregarUsuarioAsync(
            db,
            Rol.Administrador,
            nombre: "Administrador");

        var service = new PermisoService(db);

        foreach (var permiso in Enum.GetValues<Permiso>())
        {
            Assert.True(
                await service.TienePermisoAsync(
                    administrador.Id,
                    permiso));
        }
    }

    [Fact]
    public async Task TienePermisoAsync_EstandarConConsultarUsuarios_SoloConcedeEsePermiso()
    {
        using var database = new TestDatabase();
        using var db = database.CreateContext();

        var estandar = await TestData.AgregarUsuarioAsync(
            db,
            Rol.Estandar,
            nombre: "Estandar");

        await TestData.AgregarPermisoAsync(
            db,
            estandar,
            Permiso.ConsultarUsuarios);

        var service = new PermisoService(db);

        Assert.True(
            await service.TienePermisoAsync(
                estandar.Id,
                Permiso.ConsultarUsuarios));

        Assert.False(
            await service.TienePermisoAsync(
                estandar.Id,
                Permiso.GestionarUsuarios));

        Assert.False(
            await service.TienePermisoAsync(
                estandar.Id,
                Permiso.RestablecerContrasenas));

        var efectivos = await service.ObtenerEfectivosAsync(
            estandar.Id);

        Assert.Equal(
            [Permiso.ConsultarUsuarios],
            efectivos);
    }

    [Fact]
    public async Task TienePermisoAsync_UsuarioInactivo_DevuelveFalse()
    {
        using var database = new TestDatabase();
        using var db = database.CreateContext();

        var estandar = await TestData.AgregarUsuarioAsync(
            db,
            Rol.Estandar,
            activo: false);

        await TestData.AgregarPermisoAsync(
            db,
            estandar,
            Permiso.ConsultarUsuarios);

        var service = new PermisoService(db);

        Assert.False(
            await service.TienePermisoAsync(
                estandar.Id,
                Permiso.ConsultarUsuarios));

        Assert.Empty(
            await service.ObtenerEfectivosAsync(estandar.Id));
    }

    [Fact]
    public async Task TienePermisoAsync_EnumInvalido_DevuelveFalse()
    {
        using var database = new TestDatabase();
        using var db = database.CreateContext();

        var estandar = await TestData.AgregarUsuarioAsync(db);

        var service = new PermisoService(db);

        Assert.False(
            await service.TienePermisoAsync(
                estandar.Id,
                (Permiso)999));
    }

    [Fact]
    public async Task ConcederAsync_PermisoDuplicado_NoCreaDuplicado()
    {
        using var database = new TestDatabase();
        using var db = database.CreateContext();

        var estandar = await TestData.AgregarUsuarioAsync(db);

        var service = new PermisoService(db);

        var primera = await service.ConcederAsync(
            estandar.Id,
            Permiso.ConsultarUsuarios);

        var segunda = await service.ConcederAsync(
            estandar.Id,
            Permiso.ConsultarUsuarios);

        Assert.True(primera.Exito);
        Assert.True(segunda.Exito);
        Assert.NotNull(primera.PermisoUsuario);
        Assert.NotNull(segunda.PermisoUsuario);
        Assert.Equal(
            primera.PermisoUsuario.Id,
            segunda.PermisoUsuario.Id);

        Assert.Equal(
            1,
            await db.PermisosUsuario.CountAsync());
    }

    [Theory]
    [InlineData(Permiso.GestionarUsuarios)]
    [InlineData(Permiso.RestablecerContrasenas)]
    public async Task PrepararConcesionAsync_PermisoAdministrativo_NoEsSolicitable(
        Permiso permiso)
    {
        using var database = new TestDatabase();
        using var db = database.CreateContext();

        var estandar = await TestData.AgregarUsuarioAsync(db);

        var service = new PermisoService(db);

        var resultado = await service.PrepararConcesionAsync(
            estandar.Id,
            permiso);

        Assert.Equal(
            MotivoResultadoConcesionPermiso.PermisoNoSolicitable,
            resultado.Motivo);

        Assert.False(resultado.Exito);
        Assert.Empty(db.ChangeTracker.Entries<UsuarioPermiso>());
    }

    [Fact]
    public async Task PrepararConcesionAsync_EnumInvalido_DevuelvePermisoInexistente()
    {
        using var database = new TestDatabase();
        using var db = database.CreateContext();

        var estandar = await TestData.AgregarUsuarioAsync(db);

        var service = new PermisoService(db);

        var resultado = await service.PrepararConcesionAsync(
            estandar.Id,
            (Permiso)999);

        Assert.Equal(
            MotivoResultadoConcesionPermiso.PermisoInexistente,
            resultado.Motivo);

        Assert.False(resultado.Exito);
    }

    [Fact]
    public async Task PrepararConcesionAsync_UsuarioInexistente_DevuelveUsuarioNoEncontrado()
    {
        using var database = new TestDatabase();
        using var db = database.CreateContext();

        var service = new PermisoService(db);

        var resultado = await service.PrepararConcesionAsync(
            Guid.NewGuid(),
            Permiso.ConsultarUsuarios);

        Assert.Equal(
            MotivoResultadoConcesionPermiso.UsuarioNoEncontrado,
            resultado.Motivo);

        Assert.False(resultado.Exito);
    }
}
