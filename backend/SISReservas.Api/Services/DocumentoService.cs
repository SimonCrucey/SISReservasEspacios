using System.Net.Http.Headers;
using Microsoft.EntityFrameworkCore;
using SISReservas.Api.Data;
using SISReservas.Api.DTOs.Documentos;
using SISReservas.Api.Models;

namespace SISReservas.Api.Services;

public class DocumentoService(
    SISReservasDbContext db,
    IConfiguration configuration,
    IWebHostEnvironment environment,
    TimeProvider reloj,
    ILogger<DocumentoService> logger)
{
    public const long TamanoMaximo = 10 * 1024 * 1024;

    private string Directorio => Path.GetFullPath(
        configuration["Documentos:Directorio"] ?? "App_Data/documentos",
        environment.ContentRootPath);

    public async Task<DocumentoDto?> SubirAsync(Guid usuarioId, IFormFile? archivo, CancellationToken ct)
    {
        if (archivo is null || archivo.Length <= 0 || archivo.Length > TamanoMaximo)
            return null;

        var nombre = Path.GetFileName(archivo.FileName.Replace('\\', '/')).Trim();
        if (nombre.Length is 0 or > 255 || nombre is "." or ".." || nombre.Any(char.IsControl))
            return null;

        var id = Guid.NewGuid();
        var mime = MediaTypeHeaderValue.TryParse(archivo.ContentType, out var tipo)
            && tipo.MediaType is { Length: <= 127 } valor && !valor.Contains('*')
            ? valor : "application/octet-stream";
        var documento = new Documento
        {
            Id = id,
            NombreOriginal = nombre,
            NombreInterno = id.ToString("N"),
            Mime = mime,
            Tamano = archivo.Length,
            SubidoEn = reloj.GetUtcNow(),
            UsuarioId = usuarioId
        };

        var directorio = Directorio;
        Directory.CreateDirectory(directorio);
        var ruta = Path.Combine(directorio, documento.NombreInterno);
        var creado = false;
        try
        {
            await using (var destino = new FileStream(ruta, FileMode.CreateNew, FileAccess.Write, FileShare.None,
                bufferSize: 81920, useAsync: true))
            {
                creado = true;
                await archivo.CopyToAsync(destino, ct);
                if (destino.Length != documento.Tamano)
                    throw new IOException("La carga quedó incompleta.");
            }

            db.Documentos.Add(documento);
            await db.SaveChangesAsync(ct);
        }
        catch
        {
            db.Entry(documento).State = EntityState.Detached;
            if (creado)
            {
                try
                {
                    File.Delete(ruta);
                }
                catch (Exception error) when (error is IOException or UnauthorizedAccessException)
                {
                    logger.LogWarning("No se pudo limpiar el archivo de la carga {DocumentoId}.", id);
                }
            }
            throw;
        }

        return new DocumentoDto(id, nombre, mime, documento.Tamano, documento.SubidoEn, usuarioId);
    }

    public Task<List<DocumentoDto>> ListarAsync(Guid usuarioId, bool administrador, CancellationToken ct) =>
        Visibles(usuarioId, administrador).AsNoTracking()
            .OrderByDescending(d => d.SubidoEn).ThenBy(d => d.Id)
            .Select(d => new DocumentoDto(d.Id, d.NombreOriginal, d.Mime, d.Tamano, d.SubidoEn, d.UsuarioId))
            .ToListAsync(ct);

    public async Task<(Stream Contenido, string Mime, string Nombre)?> DescargarAsync(
        Guid usuarioId, bool administrador, Guid id, CancellationToken ct)
    {
        var documento = await Visibles(usuarioId, administrador).AsNoTracking()
            .SingleOrDefaultAsync(d => d.Id == id, ct);
        if (documento is null) return null;

        try
        {
            // La ruta deriva del identificador generado por el servidor.
            var archivo = File.OpenRead(Path.Combine(Directorio, documento.Id.ToString("N")));
            return (archivo, documento.Mime, documento.NombreOriginal);
        }
        catch (Exception error) when (error is FileNotFoundException or DirectoryNotFoundException)
        {
            return null;
        }
    }

    public async Task<bool> EliminarAsync(Guid usuarioId, bool administrador, Guid id, CancellationToken ct)
    {
        var documento = await Visibles(usuarioId, administrador).SingleOrDefaultAsync(d => d.Id == id, ct);
        if (documento is null) return false;

        documento.EliminadoEn = reloj.GetUtcNow();
        await db.SaveChangesAsync(ct);
        return true;
    }

    private IQueryable<Documento> Visibles(Guid usuarioId, bool administrador) =>
        db.Documentos.Where(d => d.EliminadoEn == null && (administrador || d.UsuarioId == usuarioId));
}
