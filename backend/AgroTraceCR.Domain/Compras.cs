using System.ComponentModel.DataAnnotations;

namespace AgroTraceCR.Domain;

public class ConfiguracionPago
{
    public int Id { get; set; }

    [MaxLength(56)]
    public string Destino { get; set; } = "";
    public decimal ColonesPorXlm { get; set; } = 100m;
}

public class Compra
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [MaxLength(56)]
    public string Cuenta { get; set; } = "";

    [MaxLength(56)]
    public string Destino { get; set; } = "";

    [MaxLength(28)]
    public string Memo { get; set; } = "";
    public string DetallesJson { get; set; } = "[]";
    public decimal TotalColones { get; set; }
    public decimal ColonesPorXlm { get; set; }
    public decimal MontoXlm { get; set; }
    public DateTimeOffset Fecha { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset Vence { get; set; }

    [MaxLength(20)]
    public string Estado { get; set; } = "Pendiente";

    [MaxLength(64)]
    public string? TransactionId { get; set; }

    [Timestamp]
    public byte[] Version { get; set; } = [];
}
