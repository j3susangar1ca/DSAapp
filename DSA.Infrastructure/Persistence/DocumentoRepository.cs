namespace DSA.Infrastructure.Persistence;

using System;
using System.Threading;
using System.Threading.Tasks;
using DSA.Domain.Entities;
using DSA.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

public sealed class DocumentoRepository(SiaDbContext context) : IDocumentoRepository
{
    public async Task<Documento?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await context.Documentos.FirstOrDefaultAsync(d => d.Id == id, cancellationToken);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await context.SaveChangesAsync(cancellationToken);
    }
}
