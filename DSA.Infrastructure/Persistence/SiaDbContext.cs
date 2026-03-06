namespace DSA.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;
using DSA.Domain.Entities;

public class SiaDbContext(DbContextOptions<SiaDbContext> options) : DbContext(options)
{
    public DbSet<Documento> Documentos => Set<Documento>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Documento>(entity =>
        {
            entity.HasKey(d => d.Id);
            
            // Control de Concurrencia Optimista (Protección de Mesas de Trabajo)
            entity.Property(d => d.EstadoVector)
                .IsRequired()
                .IsConcurrencyToken();

            entity.Property(d => d.Nombre).IsRequired().HasMaxLength(500);
            entity.Property(d => d.HashSHA256).IsRequired().HasMaxLength(64);
            entity.Property(d => d.PathUNC).IsRequired();

            // Restricción de Integridad Relacional: Unicidad del Nombre/Folio
            entity.HasIndex(d => d.Nombre)
                  .IsUnique()
                  .HasDatabaseName("IX_Documento_Nombre_Unique");

            // Indexación GIN para Full-Text Search en Postgres (Fase 7 del Roadmap)
            // Requiere extensión pg_trgm en PostgreSQL y paquete Npgsql.EntityFrameworkCore.PostgreSQL
            entity.HasIndex(d => d.Nombre)
                  .HasDatabaseName("IX_Documento_Nombre_GIN")
                  .HasMethod("gin")
                  .HasOperators("gin_trgm_ops"); 
        });
    }
}
