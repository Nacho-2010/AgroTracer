using System.ComponentModel.DataAnnotations;

namespace AgroTraceCR.Application;

public record NuevoLote(
    [Range(1, int.MaxValue)] int ProductoId,
    [Required, StringLength(60)] string Codigo,
    [Range(0.01, 1000000)] decimal Cantidad,
    DateOnly FechaCosecha
);

public record NuevoPedido(
    [Range(1, int.MaxValue)] int CompradorId,
    [Range(1, int.MaxValue)] int LoteId,
    [Range(0.01, 1000000)] decimal Cantidad
);

public record NuevaVaca(
    [Required, RegularExpression(@"BOV-CR-\d{4,}")] string Codigo,
    [Required, StringLength(80)] string Raza,
    [Required, StringLength(120)] string Finca,
    DateOnly FechaNacimiento,
    [Range(1, int.MaxValue)] int ProductorId
);

public record StellarReceipt(string TransactionId, DateTimeOffset Fecha);

public interface IHashService
{
    string Hash(object value);
}

public interface IStellarService
{
    Task<StellarReceipt> RegistrarAsync(string hash);
    Task<bool> VerificarAsync(string transactionId, string hash);
}

public interface ITraceService
{
    Task<object> RegistrarAsync(string tipo, int id);
    Task<object> VerificarAsync(string tipo, int id);
}
