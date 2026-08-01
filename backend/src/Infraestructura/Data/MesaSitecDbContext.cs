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

        //usuario
        modelBuilder.Entity<Usuario>(usuario =>
        {
            //email unico
            usuario.HasIndex(u => u.Email).IsUnique();

            usuario.HasOne(u => u.Tenant).WithMany().HasForeignKey(u => u.TenantId);
        });

        modelBuilder.Entity<Solicitud>(solicitud =>
        {
            // El código se busca y filtra (RN-07). Es único POR tenant y POR año,
            // "SOL-2026-00001" puede existir en dos organizaciones distintas.
            // indico unico por TenantId y Codigo para que no se repitan codigos en la misma organizacion
            solicitud.HasIndex(s => new { s.TenantId, s.Codigo }).IsUnique();

            // Relación con Categoria (una navegación, un solo camino: sin ambigüedad).
            solicitud.HasOne(s => s.Categoria).WithMany().HasForeignKey(s => s.CategoriaId).OnDelete(DeleteBehavior.Restrict);
            
            // --- Las DOS relaciones hacia usuario
            // Solicitud apunta a Usuario por dos caminos. Solicitante y Agente.
            // Con dos caminos a la misma tabla, EF Core no puede emparejar solo
            // cuál navegación va con cuál llave: hay que decírselo a mano.

            //solicitante
            solicitud.HasOne(s => s.Solicitante).WithMany().HasForeignKey(s => s.SolicitanteId).OnDelete(DeleteBehavior.Restrict);

            //agente
            solicitud.HasOne(s => s.Agente).WithMany().HasForeignKey(s => s.AgenteId).OnDelete(DeleteBehavior.Restrict);
        });
    }
}