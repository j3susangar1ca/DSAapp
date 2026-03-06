// ─────────────────────────────────────────────────────────────────────────────
// DocumentoInMemoryRepository.cs
// DSA.Tests/Fakes/DocumentoInMemoryRepository.cs
//
// Repositorio en memoria para pruebas unitarias.
// NO usar en producción — el proyecto real usa DocumentoRepository (EF Core + PostgreSQL).
// ─────────────────────────────────────────────────────────────────────────────

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DSA.Domain.Entities;
using DSA.Domain.Interfaces;

namespace DSA.Tests.Fakes
{
    /// <summary>
    /// Implementación de IDocumentoRepository en memoria.
    /// Útil para tests unitarios que no requieren base de datos real.
    /// Thread-safe gracias a ConcurrentDictionary.
    /// </summary>
    public sealed class DocumentoInMemoryRepository : IDocumentoRepository
    {
        private readonly ConcurrentDictionary<Guid, Documento> _store = new();

        public Task<Documento?> GetByIdAsync(Guid id, CancellationToken ct = default)
        {
            _store.TryGetValue(id, out var doc);
            return Task.FromResult(doc);
        }

        public Task<IEnumerable<Documento>> GetAllAsync(CancellationToken ct = default)
        {
            return Task.FromResult(_store.Values.AsEnumerable());
        }

        public Task AddAsync(Documento documento, CancellationToken ct = default)
        {
            _store[documento.Id] = documento;
            return Task.CompletedTask;
        }

        public void Update(Documento documento)
        {
            _store[documento.Id] = documento;
        }

        public Task UpdateAsync(Documento documento)
        {
            Update(documento);
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync(CancellationToken ct = default)
        {
            // En memoria no hay transacciones — operación vacía
            return Task.CompletedTask;
        }

        public Task<int> GetCountByBitAsync(int bitIndex, CancellationToken ct = default)
        {
            int mask  = 1 << bitIndex;
            int count = _store.Values.Count(d => (d.EstadoVector & mask) != 0);
            return Task.FromResult(count);
        }

        public Task<IEnumerable<Documento>> SearchByTextAsync(string query, CancellationToken ct = default)
        {
            // Búsqueda simple por nombre — sin pg_trgm en memoria
            var resultados = _store.Values
                .Where(d => d.Nombre.Contains(query, StringComparison.OrdinalIgnoreCase));
            return Task.FromResult(resultados);
        }

        /// <summary>
        /// Registra un documento directamente. Solo para configuración de tests (Arrange).
        /// </summary>
        public void RegistrarDocumento(Documento documento) => _store[documento.Id] = documento;

        /// <summary>
        /// Limpia todos los documentos. Útil entre pruebas para evitar estado compartido.
        /// </summary>
        public void Limpiar() => _store.Clear();
    }
}