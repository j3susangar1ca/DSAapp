// ─────────────────────────────────────────────────────────────────────────────
// DispatcherQueueExtensions.cs
// DSA.Presentation/Extensions/DispatcherQueueExtensions.cs
//
// Extiende DispatcherQueue con soporte async/await para el UI Thread de WinUI 3.
// Útil cuando necesitas ESPERAR el resultado de una operación de UI, por ejemplo
// mostrar un ContentDialog desde un hilo de fondo.
//
// USO:
//   await _dispatcherQueue.EnqueueAsync(() => MiMetodoDeUI());
//   var resultado = await _dispatcherQueue.EnqueueAsync(() => ObtenerValorDeUI());
// ─────────────────────────────────────────────────────────────────────────────

using System;
using System.Threading.Tasks;
using Microsoft.UI.Dispatching;

namespace DSA.Presentation.Extensions
{
    public static class DispatcherQueueExtensions
    {
        /// <summary>
        /// Encola una acción en el UI Thread y espera su finalización.
        /// A diferencia de TryEnqueue(), este método es awaitable.
        /// </summary>
        public static Task EnqueueAsync(this DispatcherQueue dispatcher, Action action)
        {
            var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

            bool encolado = dispatcher.TryEnqueue(() =>
            {
                try
                {
                    action();
                    tcs.SetResult();
                }
                catch (Exception ex)
                {
                    tcs.SetException(ex);
                }
            });

            if (!encolado)
                tcs.SetException(new InvalidOperationException(
                    "No se pudo encolar la acción en el DispatcherQueue. " +
                    "El dispatcher puede estar detenido o el hilo no tiene acceso."));

            return tcs.Task;
        }

        /// <summary>
        /// Encola una función con valor de retorno en el UI Thread y espera su resultado.
        /// Útil para leer propiedades de controles XAML desde un hilo de fondo.
        /// </summary>
        public static Task<T> EnqueueAsync<T>(this DispatcherQueue dispatcher, Func<T> func)
        {
            var tcs = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);

            bool encolado = dispatcher.TryEnqueue(() =>
            {
                try   { tcs.SetResult(func()); }
                catch (Exception ex) { tcs.SetException(ex); }
            });

            if (!encolado)
                tcs.SetException(new InvalidOperationException(
                    "No se pudo encolar la función en el DispatcherQueue. " +
                    "El dispatcher puede estar detenido o el hilo no tiene acceso."));

            return tcs.Task;
        }
    }
}