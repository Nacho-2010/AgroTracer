using AgroTraceCR.Domain;
using Microsoft.EntityFrameworkCore;
namespace AgroTraceCR.Infrastructure;
public class AgroDbContext(DbContextOptions<AgroDbContext> options) : DbContext(options) {
 public DbSet<Usuario> Usuarios => Set<Usuario>(); public DbSet<Productor> Productores => Set<Productor>(); public DbSet<Producto> Productos => Set<Producto>(); public DbSet<Lote> Lotes => Set<Lote>(); public DbSet<Pedido> Pedidos => Set<Pedido>(); public DbSet<Vaca> Vacas => Set<Vaca>(); public DbSet<CertificadoGanado> Certificados => Set<CertificadoGanado>(); public DbSet<RegistroBlockchain> Registros => Set<RegistroBlockchain>();
 protected override void OnModelCreating(ModelBuilder b) {
 b.Entity<Usuario>().HasIndex(x=>x.Email).IsUnique(); b.Entity<Vaca>().HasIndex(x=>x.Codigo).IsUnique(); b.Entity<Lote>().HasIndex(x=>x.Codigo).IsUnique(); b.Entity<CertificadoGanado>().HasIndex(x=>x.VacaId).IsUnique();
 b.Entity<Productor>().HasOne<Usuario>().WithMany().HasForeignKey(x=>x.UsuarioId).OnDelete(DeleteBehavior.Restrict);
 b.Entity<Producto>().HasOne<Productor>().WithMany().HasForeignKey(x=>x.ProductorId).OnDelete(DeleteBehavior.Restrict);
 b.Entity<Lote>().HasOne<Producto>().WithMany().HasForeignKey(x=>x.ProductoId).OnDelete(DeleteBehavior.Restrict);
 b.Entity<Pedido>().HasOne<Lote>().WithMany().HasForeignKey(x=>x.LoteId).OnDelete(DeleteBehavior.Restrict); b.Entity<Pedido>().HasOne<Usuario>().WithMany().HasForeignKey(x=>x.CompradorId).OnDelete(DeleteBehavior.Restrict);
 b.Entity<Vaca>().HasOne<Productor>().WithMany().HasForeignKey(x=>x.ProductorId).OnDelete(DeleteBehavior.Restrict); b.Entity<CertificadoGanado>().HasOne<Vaca>().WithMany().HasForeignKey(x=>x.VacaId).OnDelete(DeleteBehavior.Restrict);
 b.Entity<Producto>().Property(x=>x.PrecioReferencia).HasPrecision(18,2); b.Entity<Lote>().Property(x=>x.Cantidad).HasPrecision(18,2); b.Entity<Pedido>().Property(x=>x.Cantidad).HasPrecision(18,2);
 b.Entity<Usuario>().HasData(new Usuario{Id=1,Nombre="Ana Solís",Email="ana@example.test",Rol=Rol.Productor},new Usuario{Id=2,Nombre="Luis Mora",Email="luis@example.test",Rol=Rol.Productor},new Usuario{Id=3,Nombre="Abasto Verde PYME",Email="pyme@example.test",Rol=Rol.CompradorPyme},new Usuario{Id=4,Nombre="Admin demo",Email="admin@example.test",Rol=Rol.Administrador});
 b.Entity<Productor>().HasData(new Productor{Id=1,UsuarioId=1,Nombre="Ana Solís",Finca="El Roble",Provincia="Cartago"},new Productor{Id=2,UsuarioId=2,Nombre="Luis Mora",Finca="La Esperanza",Provincia="Alajuela"});
 b.Entity<Producto>().HasData(new Producto{Id=1,ProductorId=1,Nombre="Café de altura",PrecioReferencia=4500},new Producto{Id=2,ProductorId=1,Nombre="Aguacate",PrecioReferencia=1800},new Producto{Id=3,ProductorId=2,Nombre="Leche fresca",Unidad="litro",PrecioReferencia=550});
 b.Entity<Lote>().HasData(new Lote{Id=1,ProductoId=1,Codigo="LOT-CR-0001",Cantidad=250,FechaCosecha=new(2026,10,1)},new Lote{Id=2,ProductoId=2,Codigo="LOT-CR-0002",Cantidad=500,FechaCosecha=new(2026,10,5)});
 b.Entity<Vaca>().HasData(new Vaca{Id=1,Codigo="BOV-CR-0001",Raza="Brahman",Finca="La Esperanza",ProductorId=2,FechaNacimiento=new(2023,3,12)},new Vaca{Id=2,Codigo="BOV-CR-0002",Raza="Jersey",Finca="El Roble",ProductorId=1,FechaNacimiento=new(2024,5,20)});
 }
}
