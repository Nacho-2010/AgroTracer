using System.ComponentModel.DataAnnotations;
using AgroTraceCR.Application;
using AgroTraceCR.Domain;
using AgroTraceCR.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AgroTraceCR.Api.Controllers;

public record ProductoInput(
    [Required] string Nombre,
    [Required] string Unidad,
    [Range(0, 1000000)] decimal PrecioReferencia,
    [Range(1, int.MaxValue)] int ProductorId
);

public record PersonaInput(
    [Required] string Nombre,
    [Required, EmailAddress] string Email,
    string Finca = "",
    string Provincia = ""
);

[ApiController, Route("api")]
public class ManagementController(AgroDbContext db) : ControllerBase
{
    [HttpGet("compradores")]
    public async Task<object> Compradores() =>
        await db
            .Usuarios.AsNoTracking()
            .Where(x => x.Rol == Rol.CompradorPyme)
            .Select(x => new
            {
                x.Id,
                x.Nombre,
                x.Email,
            })
            .ToListAsync();

    [HttpGet("certificados")]
    public async Task<object> Certificados() =>
        await (
            from c in db.Certificados.AsNoTracking()
            join v in db.Vacas on c.VacaId equals v.Id
            select new
            {
                c.Id,
                c.VacaId,
                c.Fecha,
                v.Codigo,
                v.Raza,
            }
        ).ToListAsync();

    [HttpPost("productos")]
    [HttpPut("productos/{id:int}")]
    public async Task<IActionResult> Producto(ProductoInput dto, [FromRoute] int? id = null)
    {
        if (!await db.Productores.AnyAsync(x => x.Id == dto.ProductorId))
            return BadRequest(new { title = "Productor inexistente" });
        var p = id.HasValue
            ? await db.Productos.FindAsync(id.Value) ?? throw new KeyNotFoundException()
            : new Producto();
        p.Nombre = dto.Nombre;
        p.Unidad = dto.Unidad;
        p.PrecioReferencia = dto.PrecioReferencia;
        p.ProductorId = dto.ProductorId;
        if (!id.HasValue)
            db.Productos.Add(p);
        await db.SaveChangesAsync();
        return Ok(p);
    }

    [HttpPost("compradores")]
    [HttpPut("compradores/{id:int}")]
    public async Task<IActionResult> Comprador(PersonaInput dto, [FromRoute] int? id = null)
    {
        var u = id.HasValue
            ? await db.Usuarios.SingleOrDefaultAsync(x =>
                x.Id == id.Value && x.Rol == Rol.CompradorPyme
            ) ?? throw new KeyNotFoundException()
            : new Usuario { Rol = Rol.CompradorPyme };
        if (await db.Usuarios.AnyAsync(x => x.Email == dto.Email && x.Id != u.Id))
            return Conflict(new { title = "El correo ya está registrado" });
        u.Nombre = dto.Nombre;
        u.Email = dto.Email;
        if (!id.HasValue)
            db.Usuarios.Add(u);
        await db.SaveChangesAsync();
        return Ok(u);
    }

    [HttpPost("productores")]
    [HttpPut("productores/{id:int}")]
    public async Task<IActionResult> Productor(PersonaInput dto, [FromRoute] int? id = null)
    {
        if (string.IsNullOrWhiteSpace(dto.Finca) || string.IsNullOrWhiteSpace(dto.Provincia))
            return BadRequest(new { title = "Indica finca y provincia" });
        var p = id.HasValue
            ? await db.Productores.FindAsync(id.Value) ?? throw new KeyNotFoundException()
            : new Productor();
        var u = id.HasValue
            ? await db.Usuarios.FindAsync(p.UsuarioId) ?? throw new KeyNotFoundException()
            : new Usuario { Rol = Rol.Productor };
        if (await db.Usuarios.AnyAsync(x => x.Email == dto.Email && x.Id != u.Id))
            return Conflict(new { title = "El correo ya está registrado" });
        await using var tx = await db.Database.BeginTransactionAsync();
        u.Nombre = dto.Nombre;
        u.Email = dto.Email;
        if (!id.HasValue)
        {
            db.Usuarios.Add(u);
            await db.SaveChangesAsync();
            p.UsuarioId = u.Id;
            db.Productores.Add(p);
        }
        p.Nombre = dto.Nombre;
        p.Finca = dto.Finca;
        p.Provincia = dto.Provincia;
        await db.SaveChangesAsync();
        await tx.CommitAsync();
        return Ok(p);
    }

    [HttpPut("lotes/{id:int}")]
    public async Task<IActionResult> Lote(int id, NuevoLote dto)
    {
        var l = await db.Lotes.FindAsync(id) ?? throw new KeyNotFoundException();
        if (!await db.Productos.AnyAsync(x => x.Id == dto.ProductoId))
            return BadRequest(new { title = "Producto inexistente" });
        if (await db.Lotes.AnyAsync(x => x.Id != id && x.Codigo == dto.Codigo))
            return Conflict(new { title = "Código duplicado" });
        if (await db.Pedidos.AnyAsync(x => x.LoteId == id && x.Cantidad > dto.Cantidad))
            return Conflict(new { title = "La cantidad es menor que un pedido existente" });
        l.ProductoId = dto.ProductoId;
        l.Codigo = dto.Codigo;
        l.Cantidad = dto.Cantidad;
        l.FechaCosecha = dto.FechaCosecha;
        await db.SaveChangesAsync();
        return Ok(l);
    }

    [HttpPut("pedidos/{id:int}")]
    public async Task<IActionResult> Pedido(int id, NuevoPedido dto)
    {
        var p = await db.Pedidos.FindAsync(id) ?? throw new KeyNotFoundException();
        var l = await db.Lotes.FindAsync(dto.LoteId);
        if (
            l == null
            || dto.Cantidad > l.Cantidad
            || !await db.Usuarios.AnyAsync(x =>
                x.Id == dto.CompradorId && x.Rol == Rol.CompradorPyme
            )
        )
            return BadRequest(new { title = "Lote, cantidad o comprador inválido" });
        p.CompradorId = dto.CompradorId;
        p.LoteId = dto.LoteId;
        p.Cantidad = dto.Cantidad;
        await db.SaveChangesAsync();
        return Ok(p);
    }

    [HttpPut("ganado/{id:int}")]
    public async Task<IActionResult> Ganado(int id, NuevaVaca dto)
    {
        var v = await db.Vacas.FindAsync(id) ?? throw new KeyNotFoundException();
        if (
            dto.FechaNacimiento > DateOnly.FromDateTime(DateTime.UtcNow)
            || !await db.Productores.AnyAsync(x => x.Id == dto.ProductorId)
        )
            return BadRequest(new { title = "Fecha o productor inválido" });
        if (await db.Vacas.AnyAsync(x => x.Id != id && x.Codigo == dto.Codigo))
            return Conflict(new { title = "Código bovino duplicado" });
        v.Codigo = dto.Codigo;
        v.Raza = dto.Raza;
        v.Finca = dto.Finca;
        v.FechaNacimiento = dto.FechaNacimiento;
        v.ProductorId = dto.ProductorId;
        await db.SaveChangesAsync();
        return Ok(v);
    }

    [HttpDelete(
        "{kind:regex(^(productos|productores|compradores|lotes|pedidos|ganado)$)}/{id:int}"
    )]
    public async Task<IActionResult> Eliminar(string kind, int id)
    {
        bool linked = kind switch
        {
            "productos" => await db.Lotes.AnyAsync(x => x.ProductoId == id),
            "productores" => await db.Productos.AnyAsync(x => x.ProductorId == id)
                || await db.Vacas.AnyAsync(x => x.ProductorId == id),
            "compradores" => await db.Pedidos.AnyAsync(x => x.CompradorId == id),
            "lotes" => await db.Pedidos.AnyAsync(x => x.LoteId == id)
                || await db.Registros.AnyAsync(x => x.Tipo == "lote" && x.EntidadId == id),
            "pedidos" => await db.Registros.AnyAsync(x => x.Tipo == "pedido" && x.EntidadId == id),
            "ganado" => await db.Certificados.AnyAsync(x => x.VacaId == id),
            _ => false,
        };
        if (linked)
            return Conflict(
                new
                {
                    title = "No se puede eliminar: tiene registros relacionados o evidencia de trazabilidad",
                }
            );
        Entidad? entity = kind switch
        {
            "productos" => await db.Productos.FindAsync(id),
            "productores" => await db.Productores.FindAsync(id),
            "compradores" => await db.Usuarios.SingleOrDefaultAsync(x =>
                x.Id == id && x.Rol == Rol.CompradorPyme
            ),
            "lotes" => await db.Lotes.FindAsync(id),
            "pedidos" => await db.Pedidos.FindAsync(id),
            "ganado" => await db.Vacas.FindAsync(id),
            _ => null,
        };
        if (entity == null)
            return NotFound(new { title = "Registro no encontrado" });
        db.Remove(entity);
        if (entity is Productor producer)
        {
            var u = await db.Usuarios.FindAsync(producer.UsuarioId);
            if (u != null)
                db.Usuarios.Remove(u);
        }
        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            return Conflict(
                new { title = "No se puede eliminar porque tiene registros relacionados" }
            );
        }
        return NoContent();
    }
}
