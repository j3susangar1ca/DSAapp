// ─────────────────────────────────────────────────────────────────────────────
// DigitizationDTOs.cs
// DSA.Application/DTOs/DigitizationDTOs.cs
//
// Records inmutables para el pipeline de digitalización.
// ─────────────────────────────────────────────────────────────────────────────

namespace DSA.Application.DTOs;

using System;

/// <summary>
/// Resultado final del pipeline completo de digitalización.
/// Incluye métricas de rendimiento y el estado final del vector.
/// </summary>
public record ResultadoDigitalizacion(
    bool     Exitoso,
    string   HashSha256,
    string   RutaAlmacenada,
    int      PaginasProcesadas,
    int      PaginasDescartadas,
    ushort   EstadoVectorFinal,
    TimeSpan DuracionTotal);

/// <summary>
/// Progreso reportado al UI Thread durante cada fase del pipeline.
/// </summary>
public record ProgresoDigitalizacion(
    string Mensaje,
    int    Porcentaje,
    string Fase);
