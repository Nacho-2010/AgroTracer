using AgroTraceCR.Application;
using AgroTraceCR.Domain;
using Microsoft.EntityFrameworkCore;

namespace AgroTraceCR.Infrastructure;

public class TraceService(AgroDbContext db, IHashService hashes, IStellarService stellar)
    : ITraceService
{
    async Task<object> Payload(string tipo, int id)
    {
        switch (tipo)
        {
            case "lote":
                var l = await db.Lotes.FindAsync(id) ?? throw new KeyNotFoundException();
                return new
                {
                    schema = 1,
                    tipo,
                    l.Id,
                    l.ProductoId,
                    l.Codigo,
                    l.Cantidad,
                    l.FechaCosecha,
                };
            case "pedido":
                var p = await db.Pedidos.FindAsync(id) ?? throw new KeyNotFoundException();
                return new
                {
                    schema = 1,
                    tipo,
                    p.Id,
                    p.CompradorId,
                    p.LoteId,
                    p.Cantidad,
                    p.Estado,
                };
            case "certificado":
                var c = await db.Certificados.FindAsync(id) ?? throw new KeyNotFoundException();
                var v = await db.Vacas.FindAsync(c.VacaId) ?? throw new KeyNotFoundException();
                return new
                {
                    schema = 1,
                    tipo,
                    c.Id,
                    c.Fecha,
                    c.Alcance,
                    v.Codigo,
                    v.Raza,
                    v.Finca,
                    v.FechaNacimiento,
                    v.Estado,
                    v.ProductorId,
                };
            default:
                throw new ArgumentException("Tipo válido: lote, pedido o certificado.");
        }
    }

    public async Task<object> RegistrarAsync(string tipo, int id)
    {
        var hash = hashes.Hash(await Payload(tipo, id));
        var existing = await db.Registros.FirstOrDefaultAsync(x =>
            x.Tipo == tipo && x.EntidadId == id && x.Hash == hash && x.Estado == "Confirmado"
        );
        if (existing != null)
            return existing;
        var r = new RegistroBlockchain
        {
            Tipo = tipo,
            EntidadId = id,
            Hash = hash,
            Fecha = DateTimeOffset.UtcNow,
        };
        db.Registros.Add(r);
        await db.SaveChangesAsync();
        try
        {
            var receipt = await stellar.RegistrarAsync(hash);
            r.TransactionId = receipt.TransactionId;
            r.Fecha = receipt.Fecha;
            r.Estado = "Confirmado";
            await db.SaveChangesAsync();
            return r;
        }
        catch
        {
            r.Estado = "Error";
            await db.SaveChangesAsync();
            throw;
        }
    }

    public async Task<object> VerificarAsync(string tipo, int id)
    {
        var actual = hashes.Hash(await Payload(tipo, id));
        var r = await db
            .Registros.Where(x => x.Tipo == tipo && x.EntidadId == id && x.Estado == "Confirmado")
            .OrderByDescending(x => x.Id)
            .FirstOrDefaultAsync();
        var onChain = r?.TransactionId is string tx && await stellar.VerificarAsync(tx, r.Hash);
        return new
        {
            tipo,
            id,
            hashActual = actual,
            registro = r,
            coincide = r != null && r.Hash == actual,
            verificadoEnStellar = onChain,
            verificado = r != null && r.Hash == actual && onChain,
            explorador = r?.TransactionId is string t
                ? $"https://stellar.expert/explorer/testnet/tx/{t}"
                : null,
        };
    }
}
