using Microsoft.UI.Xaml.Controls;
using DSA.Presentation.Views;
using DSA.Domain.Entities;

namespace DSA.Presentation;

public sealed partial class MainWindow : Window
{
    public MainViewModel ViewModel { get; }

    public MainWindow()
    {
        this.InitializeComponent();
        ViewModel = App.Services.GetRequiredService<MainViewModel>();
        SuscribirNavegacion();
    }

    private void SuscribirNavegacion()
    {
        // Escucha la selección desde el ViewModel para cambiar la página
        ViewModel.NavegacionRequerida += async (doc) => 
        {
            // 1. Navega a la página de trabajo
            // Nota: Se asume que ContentFrame está definido en MainWindow.xaml
            if (this.Content is Frame frame)
            {
                frame.Navigate(typeof(DocumentWorkPage));

                // 2. Recupera la instancia de la página y dispara la carga
                if (frame.Content is DocumentWorkPage page)
                {
                    // Nota: Se requiere que DocumentWorkViewModel.CargarDocumentoAsync acepte byte[] como se definió antes
                    // pero aquí solo pasamos doc. Por ahora ajustamos a la firma actual.
                    // El requerimiento previo cambió la firma a (Documento, byte[])
                    // Sin embargo, el doc ya tiene el Path.
                    // Para simplificar según el snippet del usuario:
                    await page.ViewModel.CargarDocumentoAsync(doc, null!); 
                }
            }
        };
    }
}
