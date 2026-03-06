// ─────────────────────────────────────────────────────────────────────────────
// CapturaViewModel.cs
// DSA.Presentation/ViewModels/CapturaViewModel.cs
//
// PATRONES IMPLEMENTADOS:
//   ✓ ObservableObject (MVVM Toolkit) — compatible con tu proyecto DSA
//   ✓ RelayCommand — reemplaza INotifyPropertyChanged manual
//   ✓ CancellationTokenSource — cancelación por el usuario
//   ✓ IProgress<T> — marshaling seguro al UI Thread vía DispatcherQueue
// ─────────────────────────────────────────────────────────────────────────────

using System;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Dispatching;
using Microsoft.Extensions.Logging;
using DSA.Domain.Entities;
using DSA.Application.Services;
using DSA.Application.DTOs;       // ResultadoDigitalizacion, ProgresoDigitalizacion
using DSA.Application.Exceptions; // DigitalizacionException

namespace DSA.Presentation.ViewModels
{
    public sealed partial class CapturaViewModel : ObservableObject, IDisposable
    {
        // ─── Dependencias ─────────────────────────────────────────────────────────
        private readonly DigitizationService              _digitizationService;
        private readonly ILogger<CapturaViewModel>        _logger;
        private readonly DispatcherQueue                  _dispatcherQueue;

        // ─── Estado interno ───────────────────────────────────────────────────────
        private CancellationTokenSource? _cts;
        private bool                     _disposed;

        // ─── Propiedades Observables (MVVM Toolkit genera el boilerplate) ─────────
        [ObservableProperty]
        private string  _mensajeEstado    = "Listo para digitalizar.";

        [ObservableProperty]
        private string  _faseActual       = string.Empty;

        [ObservableProperty]
        private int     _porcentaje       = 0;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(PuedeCancelar))]
        [NotifyPropertyChangedFor(nameof(PuedeIniciar))]
        private bool    _estaEscaneando   = false;

        [ObservableProperty]
        private bool    _exitoso          = false;

        [ObservableProperty]
        private string? _hashResultado;

        [ObservableProperty]
        private string? _rutaResultado;

        [ObservableProperty]
        private int     _paginasProcesadas;

        // ─── Propiedades calculadas ───────────────────────────────────────────────
        public bool PuedeCancelar => EstaEscaneando && _cts != null && !_cts.IsCancellationRequested;
        public bool PuedeIniciar  => !EstaEscaneando;

        // ─── Constructor ──────────────────────────────────────────────────────────
        public CapturaViewModel(
            DigitizationService       digitizationService,
            ILogger<CapturaViewModel> logger)
        {
            _digitizationService = digitizationService
                ?? throw new ArgumentNullException(nameof(digitizationService));
            _logger = logger
                ?? throw new ArgumentNullException(nameof(logger));

            // Debe instanciarse en el UI Thread
            _dispatcherQueue = DispatcherQueue.GetForCurrentThread()
                ?? throw new InvalidOperationException(
                    "CapturaViewModel debe instanciarse en el UI Thread de WinUI 3.");
        }

        // ─── Comando Principal: Digitalizar ───────────────────────────────────────

        /// <summary>
        /// Inicia el pipeline completo. Vinculado al botón "Digitalizar" en XAML.
        /// Uso: Command="{x:Bind ViewModel.IniciarDigitalizacionCommand}"
        /// </summary>
        [RelayCommand]
        public async Task IniciarDigitalizacionAsync(Documento documento)
        {
            if (EstaEscaneando) return;

            ResetearEstadoUI();
            EstaEscaneando = true;

            _cts = new CancellationTokenSource();

            // IProgress<T> captura el SynchronizationContext del UI Thread al construirse.
            // El callback ActualizarProgresoEnUI siempre se ejecuta en el UI Thread.
            var progress = new Progress<ProgresoDigitalizacion>(ActualizarProgresoEnUI);

            try
            {
                var resultado = await _digitizationService.DigitalizarAsync(
                    documentoId:     documento.Id,
                    opcionesEscaneo: null,        // Usa OpcionesEscaneo.Institucional (300 DPI, ADF, Gris)
                    operador:        "Operador",  // TODO: reemplazar con usuario autenticado real
                    uiProgress:      progress,
                    ct:              _cts.Token);

                _dispatcherQueue.TryEnqueue(() =>
                {
                    HashResultado     = resultado.HashSha256;
                    RutaResultado     = resultado.RutaAlmacenada;
                    PaginasProcesadas = resultado.PaginasProcesadas;
                    Exitoso           = true;
                    MensajeEstado     = $"✓ Documento sellado — {resultado.PaginasProcesadas} páginas — " +
                                        $"Hash: {resultado.HashSha256[..8]}...";
                    FaseActual        = "COMPLETADO";
                    Porcentaje        = 100;
                });

                _logger.LogInformation(
                    "Digitalización exitosa. Documento={Id} Páginas={P} Duración={D}ms",
                    documento.Id,
                    resultado.PaginasProcesadas,
                    resultado.DuracionTotal.TotalMilliseconds);
            }
            catch (OperationCanceledException)
            {
                _dispatcherQueue.TryEnqueue(() =>
                {
                    MensajeEstado = "⊘ Digitalización cancelada.";
                    FaseActual    = "CANCELADO";
                    Porcentaje    = 0;
                    Exitoso       = false;
                });
            }
            catch (DigitalizacionException ex)
            {
                _dispatcherQueue.TryEnqueue(() =>
                {
                    MensajeEstado = $"✗ Error en fase [{ex.Fase}]: {ex.Message}";
                    FaseActual    = "ERROR";
                    Exitoso       = false;
                });

                _logger.LogError(ex, "Fallo en pipeline para documento {Id}", documento.Id);
            }
            catch (Exception ex)
            {
                _dispatcherQueue.TryEnqueue(() =>
                {
                    MensajeEstado = $"✗ Error inesperado: {ex.Message}";
                    FaseActual    = "ERROR";
                    Exitoso       = false;
                });

                _logger.LogError(ex, "Error inesperado durante digitalización.");
            }
            finally
            {
                _cts?.Dispose();
                _cts = null;

                _dispatcherQueue.TryEnqueue(() =>
                {
                    EstaEscaneando = false;
                    // MVVM Toolkit notifica PuedeCancelar y PuedeIniciar
                    // automáticamente gracias a [NotifyPropertyChangedFor]
                });
            }
        }

        // ─── Comando: Cancelar ────────────────────────────────────────────────────

        /// <summary>
        /// Cancela el escaneo en curso.
        /// Uso: Command="{x:Bind ViewModel.CancelarDigitalizacionCommand}"
        /// </summary>
        [RelayCommand]
        public void CancelarDigitalizacion()
        {
            if (_cts == null || _cts.IsCancellationRequested) return;

            _logger.LogWarning("Cancelación solicitada por el usuario.");
            _cts.Cancel();
            OnPropertyChanged(nameof(PuedeCancelar));
        }

        // ─── Privados ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Callback de IProgress. Siempre ejecutado en el UI Thread.
        /// No necesita DispatcherQueue manual — IProgress lo maneja.
        /// </summary>
        private void ActualizarProgresoEnUI(ProgresoDigitalizacion p)
        {
            MensajeEstado = p.Mensaje;
            Porcentaje    = p.Porcentaje;
            FaseActual    = p.Fase;
        }

        private void ResetearEstadoUI()
        {
            MensajeEstado     = "Iniciando pipeline de digitalización...";
            FaseActual        = string.Empty;
            Porcentaje        = 0;
            Exitoso           = false;
            HashResultado     = null;
            RutaResultado     = null;
            PaginasProcesadas = 0;
        }

        // ─── IDisposable ──────────────────────────────────────────────────────────

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _cts?.Cancel();
            _cts?.Dispose();
        }
    }
}