using AgroTraceCR.Application;
using AgroTraceCR.Domain;
using Microsoft.EntityFrameworkCore;

namespace AgroTraceCR.Infrastructure;

public class TraceService(
    AgroDbContext db,
    IHashService hashes,
    IStellarService stellar,
    ISorobanService soroban
) : ITraceService
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

    static readonly SemaphoreSlim SigningGate = new(1, 1);

    public async Task<object> RegistrarAsync(string tipo, int id)
    {
        await SigningGate.WaitAsync();
        try
        {
            var hash = hashes.Hash(await Payload(tipo, id));
            var contractId = soroban.ContractId;
            var pending = await db
                .Registros.Where(x =>
                    x.Tipo == tipo
                    && x.EntidadId == id
                    && x.ContractId == contractId
                    && x.Estado == "Pendiente"
                    && x.TransactionId != null
                )
                .OrderByDescending(x => x.Id)
                .FirstOrDefaultAsync();
            if (pending?.TransactionId is string pendingTx)
            {
                var status = await soroban.TransactionAsync(pendingTx);
                if (status.Status == "SUCCESS" && status.Entry?.Hash == pending.Hash)
                {
                    pending.ContractVersion = status.Entry.Version;
                    pending.Estado = "Confirmado";
                    await db.SaveChangesAsync();
                }
                else if (
                    status.Status == "NOT_FOUND"
                    && pending.Fecha > DateTimeOffset.UtcNow.AddMinutes(-3)
                )
                    throw new InvalidOperationException(
                        "La transaccion del contrato sigue pendiente. Espera y vuelve a registrar para reconciliar el mismo intento."
                    );
                else
                {
                    pending.Estado = "Error";
                    await db.SaveChangesAsync();
                }
            }
            var existing = await db
                .Registros.OrderByDescending(x => x.Id)
                .FirstOrDefaultAsync(x =>
                    x.Tipo == tipo
                    && x.EntidadId == id
                    && x.ContractId == contractId
                    && x.Estado == "Confirmado"
                );
            if (existing?.Hash == hash)
            {
                var evidence = await soroban.ReadAsync(tipo, id, contractId);
                if (evidence?.Hash == hash && evidence.Version == existing.ContractVersion)
                    return existing;
            }
            var r = new RegistroBlockchain
            {
                Tipo = tipo,
                EntidadId = id,
                Hash = hash,
                ContractId = contractId,
                Fecha = DateTimeOffset.UtcNow,
            };
            db.Registros.Add(r);
            await db.SaveChangesAsync();
            try
            {
                var receipt = await soroban.RegisterAsync(tipo, id, hash);
                if (receipt.ContractId != contractId || receipt.Entry.Hash != hash)
                    throw new InvalidOperationException(
                        "La evidencia devuelta por Soroban no coincide con el registro enviado."
                    );
                r.TransactionId = receipt.TransactionId;
                r.ContractVersion = receipt.Entry.Version;
                r.Fecha = DateTimeOffset.FromUnixTimeSeconds(receipt.Entry.Timestamp);
                r.Estado = "Confirmado";
                await db.SaveChangesAsync();
                return r;
            }
            catch (SorobanSubmissionException e)
            {
                r.TransactionId = e.TransactionId;
                r.Estado = e.Pending ? "Pendiente" : "Error";
                await db.SaveChangesAsync();
                throw;
            }
            catch
            {
                r.Estado = "Error";
                await db.SaveChangesAsync();
                throw;
            }
        }
        finally
        {
            SigningGate.Release();
        }
    }

    public async Task<object> VerificarAsync(string tipo, int id)
    {
        var actual = hashes.Hash(await Payload(tipo, id));
        var r = await db
            .Registros.Where(x => x.Tipo == tipo && x.EntidadId == id && x.Estado == "Confirmado")
            .OrderByDescending(x => x.Id)
            .FirstOrDefaultAsync();
        SorobanEntry? entry = null;
        bool onChain;
        if (r?.ContractId is string contractId && r.ContractVersion is int version)
        {
            entry = await soroban.ReadAsync(tipo, id, contractId, version);
            onChain = entry?.Hash == r.Hash && entry.Version == version;
        }
        else
            onChain = r?.TransactionId is string tx && await stellar.VerificarAsync(tx, r.Hash);
        var pending = await db
            .Registros.Where(x => x.Tipo == tipo && x.EntidadId == id && x.Estado == "Pendiente")
            .OrderByDescending(x => x.Id)
            .Select(x => new { x.TransactionId, x.Fecha })
            .FirstOrDefaultAsync();
        return new
        {
            tipo,
            id,
            hashActual = actual,
            registro = r,
            coincide = r != null && r.Hash == actual,
            verificadoEnStellar = onChain,
            verificado = r != null && r.Hash == actual && onChain,
            mecanismo = r?.ContractId != null ? "Soroban" : "Stellar Classic",
            contrato = r?.ContractId,
            versionContrato = r?.ContractVersion,
            evidenciaContrato = entry,
            pendiente = pending,
            explorador = r?.TransactionId is string t
                ? $"https://stellar.expert/explorer/testnet/tx/{t}"
                : null,
            exploradorContrato = r?.ContractId is string c
                ? $"https://stellar.expert/explorer/testnet/contract/{c}"
                : null,
        };
    }
}
