using System;
using Microsoft.UI.Xaml.Controls;
using Windows.Storage.Pickers;
using DSAapp.ViewModels;

namespace DSAapp.Views;

public sealed partial class OficiosPage : Page
{
    public OficiosViewModel ViewModel
    {
        get;
    }

    public OficiosPage()
    {
        // Conectamos el ViewModel usando la inyección de dependencias de Template Studio
        ViewModel = App.GetService<OficiosViewModel>();
        this.InitializeComponent();
    }

    // Este método se ejecuta cuando el usuario hace clic en "Seleccionar PDF..."
    private async void SeleccionarPdf_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        // 1. Creamos el selector de archivos
        var picker = new FileOpenPicker();

        // 2. TRUCO DE WINUI 3: Le decimos al selector a qué ventana pertenece
        // (App.MainWindow es la ventana principal que Template Studio crea en App.xaml.cs)
        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow);
        WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

        // 3. Configuramos qué tipo de archivos permitimos
        picker.ViewMode = PickerViewMode.List;
        picker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
        picker.FileTypeFilter.Add(".pdf");

        // 4. Abrimos la ventana y esperamos a que el usuario elija un archivo
        var archivo = await picker.PickSingleFileAsync();

        if (archivo != null)
        {
            // Si eligió un archivo, guardamos la ruta en el ViewModel
            ViewModel.RutaArchivoLocal = archivo.Path;
        }
        else
        {
            // Si canceló la ventana, limpiamos la ruta
            ViewModel.RutaArchivoLocal = "Operación cancelada.";
        }
    }
}