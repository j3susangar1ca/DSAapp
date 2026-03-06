using System;

namespace DSA.Application.DTOs
{
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
        Color = 3 
    }

    public sealed class OpcionesEscaneo
    {
        public string? DispositivoId { get; init; }
        public ModoColor ModoColor { get; init; } = ModoColor.BlancoYNegro;
        public int ResolucionDpi { get; init; } = 300;
        public double UmbralPaginaBlanco { get; init; } = 0.98;

        public static OpcionesEscaneo Institucional => new()
        {
            ModoColor = ModoColor.BlancoYNegro,
            ResolucionDpi = 300,
            UmbralPaginaBlanco = 0.98
        };
    }

    // ─── Excepciones del Dominio Hardware ───────────────────────────

    public abstract class EscanerException : Exception
    {
        public string? DeviceId { get; }
        protected EscanerException(string message, string? deviceId = null, Exception? inner = null)
            : base(message, inner) => DeviceId = deviceId;
    }

    public sealed class EscanerNoDisponibleException : EscanerException
    {
        public EscanerNoDisponibleException(string message, string? deviceId = null)
            : base(message, deviceId) { }
    }

    public sealed class EscanerOperacionException : EscanerException
    {
        public int? CodigoErrorCom { get; }
        public EscanerOperacionException(string message, Exception inner, string? deviceId = null, int? codigoErrorCom = null)
            : base(message, deviceId, inner) => CodigoErrorCom = codigoErrorCom;
    }

    public sealed class AlimentadorVacioException : EscanerException
    {
        public AlimentadorVacioException(string? deviceId = null)
            : base("El alimentador automático está vacío. Inserte hojas e intente nuevamente.", deviceId) { }
    }
}
