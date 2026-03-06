using Microsoft.EntityFrameworkCore;

namespace SIA.Infrastructure.Persistence;

public class SiaDbContext(DbContextOptions<SiaDbContext> options) : DbContext(options)
{
    public DbSet<Documento> Documentos => Set<Documento>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Documento>().HasKey(d => d.Id);
        modelBuilder.Entity<Documento>().Property(d => d.EstadoVector).IsRequired();
        // Indexación para Full-Text Search en Postgres
        modelBuilder.Entity<Documento>().HasIndex(d => d.Nombre).HasMethod("gin"); 
    }
}