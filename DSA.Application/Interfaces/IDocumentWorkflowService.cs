namespace DSA.Application.Interfaces;

using System;
using System.Threading;
using System.Threading.Tasks;

public interface IDocumentWorkflowService
{
    ValueTask EjecutarTransicionArchivadoAsync(Guid documentoId, CancellationToken cancellationToken = default);
    ValueTask ActualizarDocumentoAsync(Documento documento, CancellationToken cancellationToken = default);
}
