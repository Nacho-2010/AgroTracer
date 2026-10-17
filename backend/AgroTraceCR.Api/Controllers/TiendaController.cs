using System.ComponentModel.DataAnnotations;
using System.Net.Http.Json;
using System.Text.Json;
using AgroTraceCR.Domain;
using AgroTraceCR.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StellarDotnetSdk;
using StellarDotnetSdk.Accounts;

namespace AgroTraceCR.Api.Controllers;

public record ItemCompra(
    [Range(1, int.MaxValue)] int ProductoId,
    [Range(0.01, 1000000)] decimal Cantidad
);

public record NuevaCompra(
    [Required] string Cuenta,
    [Required, MinLength(1), MaxLength(50)] ItemCompra[] Items
);

public record ConfirmarCompra(
    [Required, RegularExpression("^[a-fA-F0-9]{64}$")] string TransactionId
);

public record ConfigurarPago(
    [Required] string Destino,
    [Range(0.01, 1000000)] decimal ColonesPorXlm
);

[ApiController, Route("api/tienda")]
public class TiendaController(AgroDbContext db) : ControllerBase
{
    static bool ValidAccount(string value)
    {
        try
        {
            return KeyPair.FromAccountId(value).AccountId == value;
        }
        catch
        {
            return false;
        }
    }

    [HttpGet("configuracion")]
    public async Task<object> Configuracion()
    {
        var c = await db.ConfiguracionPagos.FindAsync(1);
        return new
        {
            destino = c?.Destino ?? "",
            colonesPorXlm = c?.ColonesPorXlm ?? 100m,
            red = "Testnet",
            configurado = c != null && ValidAccount(c.Destino),
        };
    }

    [HttpPut("configuracion")]
    public async Task<IActionResult> Configuracion(ConfigurarPago dto)
    {
        if (!ValidAccount(dto.Destino) || decimal.Round(dto.ColonesPorXlm, 2) != dto.ColonesPorXlm)
            return BadRequest(
                new
                {
                    title = "Indica una dirección pública Stellar válida y una conversión con máximo dos decimales",
                }
            );
        var c = await db.ConfiguracionPagos.FindAsync(1);
        if (c == null)
        {
            c = new();
            db.ConfiguracionPagos.Add(c);
        }
        c.Destino = dto.Destino;
        c.ColonesPorXlm = dto.ColonesPorXlm;
        await db.SaveChangesAsync();
        return Ok(
            new
            {
                c.Destino,
                c.ColonesPorXlm,
                red = "Testnet",
            }
        );
    }

    [HttpPost("compras")]
    public async Task<IActionResult> Crear(NuevaCompra dto)
    {
        if (!ValidAccount(dto.Cuenta))
            return BadRequest(new { title = "Conecta una billetera Stellar válida" });
        var config = await db.ConfiguracionPagos.FindAsync(1);
        if (config == null || !ValidAccount(config.Destino))
            return StatusCode(
                503,
                new { title = "El administrador debe configurar la cuenta receptora de Testnet" }
            );
        if (dto.Cuenta == config.Destino)
            return BadRequest(
                new
                {
                    title = "La billetera del comprador debe ser distinta de la cuenta receptora",
                }
            );
        if (dto.Items.Any(x => decimal.Round(x.Cantidad, 2) != x.Cantidad))
            return BadRequest(new { title = "La cantidad admite hasta dos decimales" });
        var items = dto
            .Items.GroupBy(x => x.ProductoId)
            .Select(g => new ItemCompra(g.Key, g.Sum(x => x.Cantidad)))
            .ToArray();
        if (items.Any(x => x.Cantidad > 1000000))
            return BadRequest(new { title = "Cantidad fuera de rango" });
        var ids = items.Select(x => x.ProductoId).ToArray();
        var products = await db.Productos.Where(x => ids.Contains(x.Id)).ToListAsync();
        if (products.Count != items.Length || products.Any(x => x.PrecioReferencia <= 0))
            return BadRequest(new { title = "Hay productos inexistentes o sin precio disponible" });
        var details = items
            .Select(x =>
            {
                var p = products.Single(p => p.Id == x.ProductoId);
                return new
                {
                    productoId = p.Id,
                    nombre = p.Nombre,
                    unidad = p.Unidad,
                    cantidad = x.Cantidad,
                    precioColones = p.PrecioReferencia,
                    subtotal = decimal.Round(
                        p.PrecioReferencia * x.Cantidad,
                        2,
                        MidpointRounding.AwayFromZero
                    ),
                };
            })
            .ToArray();
        var total = details.Sum(x => x.subtotal);
        if (total <= 0 || total > 1000000000 || total / config.ColonesPorXlm > 1000000000)
            return BadRequest(new { title = "El total debe estar entre ₡0,01 y ₡1.000.000.000" });
        var now = DateTimeOffset.UtcNow;
        var order = new Compra
        {
            Cuenta = dto.Cuenta,
            Destino = config.Destino,
            ColonesPorXlm = config.ColonesPorXlm,
            TotalColones = total,
            MontoXlm = decimal.Ceiling(total / config.ColonesPorXlm * 10000000) / 10000000,
            DetallesJson = JsonSerializer.Serialize(details),
            Fecha = now,
            Vence = now.AddMinutes(15),
        };
        order.Memo = "AGRO-" + order.Id.ToString("N")[..20];
        db.Compras.Add(order);
        await db.SaveChangesAsync();
        return Ok(View(order));
    }

    static object View(Compra c) =>
        new
        {
            c.Id,
            c.Cuenta,
            c.Destino,
            c.Memo,
            c.TotalColones,
            c.ColonesPorXlm,
            montoXlm = c.MontoXlm.ToString(
                "0.0000000",
                System.Globalization.CultureInfo.InvariantCulture
            ),
            c.Fecha,
            c.Vence,
            c.Estado,
            c.TransactionId,
            items = JsonSerializer.Deserialize<JsonElement>(c.DetallesJson),
            red = "Testnet",
            explorador = c.TransactionId == null
                ? null
                : $"https://stellar.expert/explorer/testnet/tx/{c.TransactionId}",
        };

    [HttpGet("compras")]
    public async Task<object> Compras()
    {
        var rows = await db
            .Compras.AsNoTracking()
            .OrderByDescending(x => x.Fecha)
            .Take(100)
            .ToListAsync();
        return rows.Select(View).ToArray();
    }

    [HttpGet("compras/{id:guid}")]
    public async Task<object> Compra(Guid id) =>
        View(
            await db.Compras.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id)
                ?? throw new KeyNotFoundException()
        );

    [HttpPost("compras/{id:guid}/confirmar")]
    public async Task<IActionResult> Confirmar(Guid id, ConfirmarCompra dto)
    {
        var order =
            await db.Compras.SingleOrDefaultAsync(x => x.Id == id)
            ?? throw new KeyNotFoundException();
        var hash = dto.TransactionId.ToLowerInvariant();
        if (order.Estado == "Pagado")
            return order.TransactionId == hash
                ? Ok(View(order))
                : Conflict(new { title = "Esta compra ya tiene otro pago confirmado" });
        if (await db.Compras.AnyAsync(x => x.TransactionId == hash && x.Id != id))
            return Conflict(new { title = "Esta transacción ya se utilizó en otra compra" });
        using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(25) };
        try
        {
            var response = await client.GetAsync($"{StellarService.Horizon}/transactions/{hash}");
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                return Conflict(
                    new
                    {
                        title = "El pago todavía no aparece en Testnet. Espera unos segundos y vuelve a verificar",
                    }
                );
            response.EnsureSuccessStatusCode();
            var tx = await response.Content.ReadFromJsonAsync<JsonElement>();
            var ops = await client.GetFromJsonAsync<JsonElement>(
                $"{StellarService.Horizon}/transactions/{hash}/operations?limit=200"
            );
            if (!PaymentEvidence.Matches(order, tx, ops))
                return BadRequest(
                    new
                    {
                        title = "La transacción no coincide con la cuenta, destino, monto, memo o vigencia de esta compra",
                    }
                );
        }
        catch (HttpRequestException)
        {
            return StatusCode(
                503,
                new
                {
                    title = "No se pudo consultar Stellar Testnet. Conserva el hash y vuelve a verificar; no envíes otro pago",
                }
            );
        }
        catch (TaskCanceledException)
        {
            return StatusCode(
                503,
                new { title = "Stellar tardó en responder. Vuelve a verificar el mismo pago" }
            );
        }
        order.Estado = "Pagado";
        order.TransactionId = hash;
        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            db.ChangeTracker.Clear();
            var current = await db.Compras.AsNoTracking().SingleAsync(x => x.Id == id);
            if (current.TransactionId == hash)
                return Ok(View(current));
            return Conflict(
                new { title = "El estado cambió. Consulta la compra antes de continuar" }
            );
        }
        return Ok(View(order));
    }
}
