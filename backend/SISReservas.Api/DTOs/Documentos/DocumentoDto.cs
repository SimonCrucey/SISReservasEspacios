namespace SISReservas.Api.DTOs.Documentos;

public record DocumentoDto(
    Guid Id, string NombreOriginal, string Mime, long Tamano,
    DateTimeOffset SubidoEn, Guid UsuarioId);
