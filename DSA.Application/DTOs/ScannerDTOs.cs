namespace DSA.Application.DTOs;

public sealed record InfoDispositivo(
    string Id,
    string Nombre,
    string Descripcion);

public sealed record ProgresoEscaneo(
    int PaginaActual,
    int TotalPaginasEstimadas,
    string Mensaje,
    double PorcentajeGlobal);

public enum ModoColor
{
    BlancoYNegro = 1,
    EscalaGrises = 2,
    Color        = 3
}

public sealed class OpcionesEscaneo
{
    public string?  DispositivoId      { get; init; }
    public ModoColor ModoColor         { get; init; } = ModoColor.BlancoYNegro;
    public int       ResolucionDpi     { get; init; } = 300;
    public double    UmbralPaginaBlanco{ get; init; } = 0.98;

    public static OpcionesEscaneo Institucional => new()
    {
        ModoColor          = ModoColor.BlancoYNegro,
        ResolucionDpi      = 300,
        UmbralPaginaBlanco = 0.98
    };
}

// ─── Excepciones Específicas del Escáner ───

public class EscanerNoDisponibleException : System.Exception
{
    public EscanerNoDisponibleException(string mensaje) : base(mensaje) { }
    public EscanerNoDisponibleException(string mensaje, System.Exception inner) : base(mensaje, inner) { }
}

public class AtascoPapelException : System.Exception
{
    public AtascoPapelException(string mensaje) : base(mensaje) { }
}
