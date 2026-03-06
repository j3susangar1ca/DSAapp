namespace DSA.Infrastructure.Persistence;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using DSA.Domain.Entities;
using DSA.Domain.Interfaces;

public sealed class DocumentoRepository(SiaDbContext context) : IDocumentoRepository
{
    public async Task<Documento?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await context.Documentos.FirstOrDefaultAsync(d => d.Id == id, ct);
    }

    public async Task AddAsync(Documento documento, CancellationToken ct = default)
    {
        await context.Documentos.AddAsync(documento, ct);
    }

    public async Task SaveChangesAsync(CancellationToken ct = default)
    {
        try 
        {
            await context.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            // Manejo de colisión de estados (Karnaugh)
            throw new InvalidOperationException("Error de Concurrencia: El estado del documento fue alterado por otra mesa de trabajo.");
        }
    }

    public async Task<IEnumerable<Documento>> GetAllAsync(CancellationToken ct = default)
    {
        return await context.Documentos.AsNoTracking().ToListAsync(ct);
    }
}
