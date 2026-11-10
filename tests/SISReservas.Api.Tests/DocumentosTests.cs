using System.Security.Claims;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging.Abstractions;
using SISReservas.Api.Controllers;
using SISReservas.Api.Data;
using SISReservas.Api.DTOs.Documentos;
using SISReservas.Api.Models.Enums;
using SISReservas.Api.Services;

namespace SISReservas.Api.Tests;

public class DocumentosTests : IDisposable
{
    private readonly TestDatabase database = new();
    private readonly string directorio = Path.Combine(Path.GetTempPath(), $"DocumentosTests-{Guid.NewGuid():N}");
    private readonly CancellationToken ct = CancellationToken.None;

    [Fact]
    public async Task Subir_GuardaMetadatosYContenidoConNombreInterno()
    {
        var db = database.CreateContext();
        var propietario = await TestData.AgregarUsuarioAsync(db);

        var dto = await Subir(db, propietario.Id);

        var registro = Assert.Single(db.Documentos);
        Assert.Equal(propietario.Id, dto.UsuarioId);
        Assert.Equal("prueba.txt", dto.NombreOriginal);
        Assert.Equal("text/plain", dto.Mime);
        Assert.Equal(3, dto.Tamano);
        Assert.NotEqual(default, dto.SubidoEn);
        Assert.Equal(dto.Id.ToString("N"), registro.NombreInterno);
        Assert.Null(registro.EliminadoEn);
        Assert.Equal(new byte[] { 1, 2, 3 }, File.ReadAllBytes(Path.Combine(directorio, registro.NombreInterno)));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Subir_RechazaArchivoAusenteOVacio(bool vacio)
    {
        var db = database.CreateContext();
        var usuario = await TestData.AgregarUsuarioAsync(db);
        using var contenido = new MemoryStream();
        var archivo = vacio ? new FormFile(contenido, 0, 0, "archivo", "vacio.txt") : null;

        var respuesta = await Controller(db, usuario.Id).Subir(archivo, ct);

        Assert.IsType<BadRequestObjectResult>(respuesta);
        Assert.Empty(db.Documentos);
    }

    [Fact]
    public async Task SinSesion_RechazaLasCuatroOperaciones()
    {
        var controller = Controller(database.CreateContext(), null);
        Assert.IsType<UnauthorizedResult>(await controller.Subir(null, ct));
        Assert.IsType<UnauthorizedResult>(await controller.Listar(ct));
        Assert.IsType<UnauthorizedResult>(await controller.Descargar(Guid.NewGuid(), ct));
        Assert.IsType<UnauthorizedResult>(await controller.Eliminar(Guid.NewGuid(), ct));
    }

    [Theory]
    [InlineData(Rol.Estandar, 1)]
    [InlineData(Rol.Administrador, 2)]
    public async Task Listar_FiltraSegunRol(Rol rol, int total)
    {
        var db = database.CreateContext();
        var usuario = await TestData.AgregarUsuarioAsync(db, rol);
        var otro = await TestData.AgregarUsuarioAsync(db);
        var propio = await Subir(db, usuario.Id);
        await Subir(db, otro.Id);

        var resultado = Assert.IsType<OkObjectResult>(await Controller(db, usuario.Id, rol).Listar(ct));
        var documentos = Assert.IsType<List<DocumentoDto>>(resultado.Value);

        Assert.Equal(total, documentos.Count);
        Assert.Contains(documentos, d => d.Id == propio.Id);
    }

    [Fact]
    public async Task Descargar_PermitePropietarioYAdministradorYRechazaAjeno()
    {
        var db = database.CreateContext();
        var propietario = await TestData.AgregarUsuarioAsync(db);
        var otro = await TestData.AgregarUsuarioAsync(db);
        var admin = await TestData.AgregarUsuarioAsync(db, Rol.Administrador);
        var dto = await Subir(db, propietario.Id);

        var propia = Assert.IsType<FileStreamResult>(await Controller(db, propietario.Id).Descargar(dto.Id, ct));
        using var contenidoPropio = propia.FileStream;
        var administrativa = Assert.IsType<FileStreamResult>(
            await Controller(db, admin.Id, Rol.Administrador).Descargar(dto.Id, ct));
        using var contenidoAdmin = administrativa.FileStream;

        Assert.Equal("text/plain", propia.ContentType);
        Assert.Equal("prueba.txt", propia.FileDownloadName);
        Assert.Equal(1, contenidoPropio.ReadByte());
        Assert.IsType<NotFoundResult>(await Controller(db, otro.Id).Descargar(dto.Id, ct));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Eliminar_ConservaArchivoYRegistroYExcluyeDeConsultas(bool comoAdmin)
    {
        var db = database.CreateContext();
        var propietario = await TestData.AgregarUsuarioAsync(db);
        var otro = await TestData.AgregarUsuarioAsync(db);
        var admin = await TestData.AgregarUsuarioAsync(db, Rol.Administrador);
        var dto = await Subir(db, propietario.Id);
        var controllerAdmin = Controller(db, admin.Id, Rol.Administrador);

        Assert.IsType<NotFoundResult>(await Controller(db, otro.Id).Eliminar(dto.Id, ct));
        var autorizado = comoAdmin ? controllerAdmin : Controller(db, propietario.Id);
        Assert.IsType<NoContentResult>(await autorizado.Eliminar(dto.Id, ct));

        var registro = Assert.Single(database.CreateContext().Documentos);
        Assert.NotNull(registro.EliminadoEn);
        Assert.True(File.Exists(Path.Combine(directorio, registro.NombreInterno)));
        var resultado = Assert.IsType<OkObjectResult>(await controllerAdmin.Listar(ct));
        Assert.Empty(Assert.IsType<List<DocumentoDto>>(resultado.Value));
        Assert.IsType<NotFoundResult>(await controllerAdmin.Descargar(dto.Id, ct));
    }

    [Fact]
    public async Task DescargarYEliminar_DocumentoInexistente()
    {
        var db = database.CreateContext();
        var usuario = await TestData.AgregarUsuarioAsync(db);
        var controller = Controller(db, usuario.Id);
        var id = Guid.NewGuid();

        Assert.IsType<NotFoundResult>(await controller.Descargar(id, ct));
        Assert.IsType<NotFoundResult>(await controller.Eliminar(id, ct));
    }

    [Fact]
    public async Task Descargar_ArchivoFisicoAusenteDevuelve404()
    {
        var db = database.CreateContext();
        var usuario = await TestData.AgregarUsuarioAsync(db);
        var dto = await Subir(db, usuario.Id);
        File.Delete(Path.Combine(directorio, dto.Id.ToString("N")));

        Assert.IsType<NotFoundResult>(await Controller(db, usuario.Id).Descargar(dto.Id, ct));
    }

    [Fact]
    public async Task Subir_FalloAlRegistrarCompensaArchivoYDevuelve503()
    {
        var interceptor = new FailingSaveChangesInterceptor();
        var db = database.CreateContext(interceptor);
        var usuario = await TestData.AgregarUsuarioAsync(db);
        interceptor.Fail = true;
        using var contenido = new MemoryStream(new byte[] { 1 });
        var archivo = new FormFile(contenido, 0, 1, "archivo", "prueba.txt") { Headers = new HeaderDictionary() };

        var respuesta = Assert.IsType<ObjectResult>(await Controller(db, usuario.Id).Subir(archivo, ct));

        Assert.Equal(503, respuesta.StatusCode);
        Assert.Empty(Directory.GetFiles(directorio));
        Assert.Empty(db.Documentos);
    }

    private async Task<DocumentoDto> Subir(SISReservasDbContext db, Guid usuarioId)
    {
        using var contenido = new MemoryStream(new byte[] { 1, 2, 3 });
        var archivo = new FormFile(contenido, 0, 3, "archivo", @"..\..\prueba.txt")
        {
            Headers = new HeaderDictionary(),
            ContentType = "text/plain"
        };
        var resultado = Assert.IsType<CreatedAtActionResult>(await Controller(db, usuarioId).Subir(archivo, ct));
        return Assert.IsType<DocumentoDto>(resultado.Value);
    }

    private DocumentosController Controller(SISReservasDbContext db, Guid? id, Rol rol = Rol.Estandar)
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(
            new Dictionary<string, string?> { ["Documentos:Directorio"] = directorio }).Build();
        var servicio = new DocumentoService(db, config, new EntornoPruebas(), TimeProvider.System,
            NullLogger<DocumentoService>.Instance);
        var identidad = id is Guid usuario
            ? new ClaimsIdentity([new(ClaimTypes.NameIdentifier, usuario.ToString()), new(ClaimTypes.Role, rol.ToString())], "Session")
            : new ClaimsIdentity();
        return new DocumentosController(servicio)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identidad) }
            }
        };
    }

    public void Dispose()
    {
        database.Dispose();
        if (!Directory.Exists(directorio)) return;
        foreach (var archivo in Directory.GetFiles(directorio)) File.Delete(archivo);
        Directory.Delete(directorio);
    }

    private sealed class EntornoPruebas : IWebHostEnvironment
    {
        public string ApplicationName { get; set; } = "DocumentosTests";
        public string EnvironmentName { get; set; } = "Testing";
        public string ContentRootPath { get; set; } = Path.GetTempPath();
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
        public string WebRootPath { get; set; } = "";
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
    }
}
