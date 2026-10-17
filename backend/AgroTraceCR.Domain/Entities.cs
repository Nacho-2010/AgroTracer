namespace AgroTraceCR.Domain;

public enum Rol
{
    Productor,
    CompradorPyme,
    Administrador,
}

public abstract class Entidad
{
    public int Id { get; set; }
}

public class Usuario : Entidad
{
    public string Nombre { get; set; } = "";
    public string Email { get; set; } = "";
    public Rol Rol { get; set; }
}

public class Productor : Entidad
{
    public int UsuarioId { get; set; }
    public string Nombre { get; set; } = "";
    public string Finca { get; set; } = "";
    public string Provincia { get; set; } = "";
}

public class Producto : Entidad
{
    public int ProductorId { get; set; }
    public string Nombre { get; set; } = "";
    public string Unidad { get; set; } = "kg";
    public decimal PrecioReferencia { get; set; }
}

public class Lote : Entidad
{
    public int ProductoId { get; set; }
    public string Codigo { get; set; } = "";
    public decimal Cantidad { get; set; }
    public DateOnly FechaCosecha { get; set; }
}

public class Pedido : Entidad
{
    public int CompradorId { get; set; }
    public int LoteId { get; set; }
    public decimal Cantidad { get; set; }
    public string Estado { get; set; } = "Prueba";
}

public class Vaca : Entidad
{
    public string Codigo { get; set; } = "";
    public string Raza { get; set; } = "";
    public string Finca { get; set; } = "";
    public DateOnly FechaNacimiento { get; set; }
    public string Estado { get; set; } = "Activa";
    public int ProductorId { get; set; }
}

public class CertificadoGanado : Entidad
{
    public int VacaId { get; set; }
    public DateTimeOffset Fecha { get; set; }
    public string Alcance { get; set; } = "Identificación y trazabilidad digital únicamente";
}

public class RegistroBlockchain : Entidad
{
    public string Tipo { get; set; } = "";
    public int EntidadId { get; set; }
    public string Hash { get; set; } = "";
    public string? TransactionId { get; set; }
    public string Red { get; set; } = "Testnet";
    public string? ContractId { get; set; }
    public int? ContractVersion { get; set; }
    public DateTimeOffset Fecha { get; set; }
    public string Estado { get; set; } = "Pendiente";
}
