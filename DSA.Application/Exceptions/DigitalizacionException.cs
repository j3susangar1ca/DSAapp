// ─────────────────────────────────────────────────────────────────────────────
// DigitalizacionException.cs
// DSA.Application/Exceptions/DigitalizacionException.cs
//
// Excepción tipada para fallos en el pipeline de digitalización.
// ─────────────────────────────────────────────────────────────────────────────

namespace DSA.Application.Exceptions;

using System;

/// <summary>
/// Excepción lanzada cuando el pipeline de digitalización falla en alguna fase.
/// Incluye contexto sobre la fase de fallo y el documento afectado.
/// </summary>
public sealed class DigitalizacionException : Exception
{
    /// <summary>Fase del pipeline donde ocurrió el fallo (CAPTURA, PROCESAMIENTO, PDF_A, HASH, PERSISTENCIA, SEAL).</summary>
    public string Fase { get; }

    /// <summary>Id del documento que estaba siendo procesado.</summary>
    public Guid DocumentoId { get; }

    public DigitalizacionException(string fase, Guid documentoId, string message, Exception? innerException = null)
        : base(message, innerException)
    {
        Fase = fase;
        DocumentoId = documentoId;
    }
}
