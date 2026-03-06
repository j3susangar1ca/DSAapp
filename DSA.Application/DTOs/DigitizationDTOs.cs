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

/// <summary>
/// Progreso interno de la fase de escaneo (hardware).
/// </summary>
public record ProgresoEscaneo(
    string Mensaje,
    double PorcentajeGlobal);

/// <summary>
/// Opciones configurables para la fase de captura y procesamiento.
/// </summary>
public record OpcionesEscaneo
{
    /// <summary>DPI objetivo. Valor institucional: 300.</summary>
    public int Dpi { get; init; } = 300;

    /// <summary>Umbral de luminancia media para detectar páginas en blanco (0.0–1.0).</summary>
    public double UmbralPaginaBlanco { get; init; } = 0.97;

    /// <summary>Si true, convierte a escala de grises antes de generar el PDF.</summary>
    public bool ConvertirAGrises { get; init; } = false;

    /// <summary>Configuración por defecto institucional.</summary>
    public static OpcionesEscaneo Institucional => new()
    {
        Dpi = 300,
        UmbralPaginaBlanco = 0.97,
        ConvertirAGrises = false
    };
}
