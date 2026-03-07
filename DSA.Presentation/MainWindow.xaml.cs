using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Dispatching;
using Microsoft.Extensions.DependencyInjection; // Necesario para GetRequiredService
using DSA.Presentation.Views;
using DSA.Presentation.ViewModels;            // Necesario para MainViewModel
using DSA.Domain.Entities;

namespace DSA.Presentation;

public sealed partial class MainWindow : Window
{
    public MainViewModel ViewModel { get; }

    public MainWindow()
    {
        this.InitializeComponent();
        ViewModel = App.RootScope.ServiceProvider.GetRequiredService<MainViewModel>();
        SuscribirNavegacion();
    }

    private void SuscribirNavegacion()
    {
        // Escucha la selección desde el ViewModel para cambiar la página de manera asíncrona segura
        ViewModel.NavegacionRequerida += (doc) =>
        {
            this.DispatcherQueue.TryEnqueue(async () =>
            {
                // 1. Navega a la página de trabajo en el hilo de UI
                if (this.Content is Frame frame)
                {
                    frame.Navigate(typeof(DocumentWorkPage));

                    // 2. Recupera la instancia de la página y dispara la carga de metadatos en backend
                    if (frame.Content is DocumentWorkPage page)
                    {
                        // Se delega el trabajo pesado a Task.Run internamente dentro del ViewModel
                        await page.ViewModel.CargarDocumentoAsync(doc, null!);
                    }
                }
            });
        };
    }
}
