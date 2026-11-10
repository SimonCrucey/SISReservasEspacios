namespace SISReservas.Api.Models;

public class Documento
{
    public Guid Id { get; set; }
    public required string NombreOriginal { get; set; }
    public required string NombreInterno { get; set; }
    public required string Mime { get; set; }
    public long Tamano { get; set; }
    public DateTimeOffset SubidoEn { get; set; }
    public Guid UsuarioId { get; set; }
    public DateTimeOffset? EliminadoEn { get; set; }
}
