namespace DSA.Domain.Interfaces;

using DSA.Domain.Entities;
using System;
using System.Threading;
using System.Threading.Tasks;

public interface IDocumentoRepository
{
    Task<Documento?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
