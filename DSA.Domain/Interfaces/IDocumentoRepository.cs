namespace DSA.Domain.Interfaces;

using DSA.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

public interface IDocumentoRepository
{
    Task<Documento?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<Documento>> GetAllAsync(CancellationToken cancellationToken = default);
    Task AddAsync(Documento documento, CancellationToken cancellationToken = default);
    void Update(Documento documento);
    Task UpdateAsync(Documento documento);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Consulta optimizada para el Dashboard que traduce el álgebra booleana a SQL nativo.
    /// </summary>
    Task<int> GetCountByBitAsync(int bitIndex, CancellationToken cancellationToken = default);

    /// <summary>
    /// Búsqueda Full-Text optimizada usando índices GIN (pg_trgm).
    /// </summary>
    Task<IEnumerable<Documento>> SearchByTextAsync(string query, CancellationToken cancellationToken = default);
}
