using Mesasitec.Dominio.Entidades;
using Microsoft.EntityFrameworkCore;

namespace Mesasitec.Infraestructura.Data;

public class MesaSitecDbContext : DbContext
{

    public MesaSitecDbContext(DbContextOptions<MesaSitecDbContext> options)
        : base(options)
    {
    }

    // Un DbSet<T> por tabla. Es "la tabla vista como colección consultable".
    // Sobre estos harás LINQ (.Where, .OrderBy...) y EF Core lo traduce a SQL.
    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Categoria> Categorias => Set<Categoria>();
    public DbSet<Solicitud> Solicitudes => Set<Solicitud>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
    }
}