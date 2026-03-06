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
            
            // Configuración del Vector de Estado con control de concurrencia optimista
            entity.Property(d => d.EstadoVector)
                .IsRequired()
                .IsConcurrencyToken(); // [CRÍTICO] Evita colisiones entre mesas de trabajo

            entity.Property(d => d.Nombre).IsRequired().HasMaxLength(500);
            entity.Property(d => d.HashSHA256).IsRequired().HasMaxLength(64);
            entity.Property(d => d.PathUNC).IsRequired();

            // Indexación GIN para Full-Text Search en Postgres (Fase 7 del Roadmap)
            // Nota: Requiere instalar el paquete Npgsql.EntityFrameworkCore.PostgreSQL
            entity.HasIndex(d => d.Nombre)
                  .HasMethod("gin")
                  .HasOperators("gin_trgm_ops"); 
        });
    }
}