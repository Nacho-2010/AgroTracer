using AgroTraceCR.Application;
using AgroTraceCR.Domain;
using AgroTraceCR.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AgroTraceCR.Api.Controllers;

[ApiController, Route("api")]
public class AgroController(AgroDbContext db, ITraceService trace) : ControllerBase
{
    [HttpGet("productos")]
    public async Task<object> Productos() => await db.Productos.AsNoTracking().ToListAsync();

    [HttpGet("productores")]
    public async Task<object> Productores() =>
        await (
            from p in db.Productores.AsNoTracking()
            join u in db.Usuarios on p.UsuarioId equals u.Id
            select new
            {
                p.Id,
                p.UsuarioId,
                p.Nombre,
                p.Finca,
                p.Provincia,
                u.Email,
            }
        ).ToListAsync();

    [HttpGet("productores/{id:int}")]
    public async Task<object> Productor(int id) =>
        await db.Productores.FindAsync(id) ?? throw new KeyNotFoundException();

    [HttpGet("lotes")]
    public async Task<object> Lotes() => await db.Lotes.AsNoTracking().ToListAsync();

    [HttpGet("lotes/{id:int}")]
    public async Task<object> Lote(int id) =>
        await db.Lotes.FindAsync(id) ?? throw new KeyNotFoundException();

    [HttpPost("lotes")]
    public async Task<IActionResult> CrearLote(NuevoLote dto)
    {
        if (!await db.Productos.AnyAsync(x => x.Id == dto.ProductoId))
            return BadRequest(new { title = "Producto inexistente" });
        if (await db.Lotes.AnyAsync(x => x.Codigo == dto.Codigo))
            return Conflict(new { title = "Código duplicado" });
        var l = new Lote
        {
            ProductoId = dto.ProductoId,
            Codigo = dto.Codigo,
            Cantidad = dto.Cantidad,
            FechaCosecha = dto.FechaCosecha,
        };
        db.Lotes.Add(l);
        await db.SaveChangesAsync();
        return Created($"/api/lotes/{l.Id}", l);
    }

    [HttpGet("pedidos")]
    public async Task<object> Pedidos() => await db.Pedidos.AsNoTracking().ToListAsync();

    [HttpPost("pedidos")]
    public async Task<IActionResult> CrearPedido(NuevoPedido dto)
    {
        var lote = await db.Lotes.FindAsync(dto.LoteId);
        if (
            lote == null
            || dto.Cantidad > lote.Cantidad
            || !await db.Usuarios.AnyAsync(x =>
                x.Id == dto.CompradorId && x.Rol == Rol.CompradorPyme
            )
        )
            return BadRequest(new { title = "Lote, cantidad o comprador inválido" });
        var p = new Pedido
        {
            CompradorId = dto.CompradorId,
            LoteId = dto.LoteId,
            Cantidad = dto.Cantidad,
        };
        db.Pedidos.Add(p);
        await db.SaveChangesAsync();
        return Created($"/api/pedidos/{p.Id}", p);
    }

    [HttpGet("ganado")]
    public async Task<object> Ganado() => await db.Vacas.AsNoTracking().ToListAsync();

    [HttpGet("ganado/{id:int}")]
    public async Task<object> Vaca(int id) =>
        new
        {
            vaca = await db.Vacas.FindAsync(id) ?? throw new KeyNotFoundException(),
            certificado = await db.Certificados.SingleOrDefaultAsync(x => x.VacaId == id),
        };

    [HttpPost("ganado")]
    public async Task<IActionResult> CrearVaca(NuevaVaca dto)
    {
        if (
            dto.FechaNacimiento > DateOnly.FromDateTime(DateTime.UtcNow)
            || !await db.Productores.AnyAsync(x => x.Id == dto.ProductorId)
        )
            return BadRequest(new { title = "Fecha o productor inválido" });
        if (await db.Vacas.AnyAsync(x => x.Codigo == dto.Codigo))
            return Conflict(new { title = "Código bovino duplicado" });
        var v = new Vaca
        {
            Codigo = dto.Codigo,
            Raza = dto.Raza,
            Finca = dto.Finca,
            FechaNacimiento = dto.FechaNacimiento,
            ProductorId = dto.ProductorId,
        };
        db.Vacas.Add(v);
        await db.SaveChangesAsync();
        return Created($"/api/ganado/{v.Id}", v);
    }

    [HttpPost("ganado/{id:int}/certificado")]
    public async Task<object> Certificado(int id)
    {
        if (!await db.Vacas.AnyAsync(x => x.Id == id))
            throw new KeyNotFoundException();
        var c = await db.Certificados.SingleOrDefaultAsync(x => x.VacaId == id);
        if (c != null)
            return c;
        c = new CertificadoGanado { VacaId = id, Fecha = DateTimeOffset.UtcNow };
        db.Certificados.Add(c);
        await db.SaveChangesAsync();
        return c;
    }

    [HttpPost("trazabilidad/{tipo}/{id:int}")]
    public Task<object> Registrar(string tipo, int id) => trace.RegistrarAsync(tipo, id);

    [HttpGet("trazabilidad/{tipo}/{id:int}")]
    public Task<object> Verificar(string tipo, int id) => trace.VerificarAsync(tipo, id);
}
