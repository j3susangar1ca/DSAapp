using DSAapp.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage.Pickers;

namespace DSAapp.Views;

public sealed partial class OficiosPage : Page
{
    public OficiosViewModel ViewModel
    {
        get;
    }

    public OficiosPage()
    {
        ViewModel = App.GetService<OficiosViewModel>();
        InitializeComponent();
    }

    // ── Selector de archivo nativo ──
    private async void SeleccionarPdf_Click(object sender, RoutedEventArgs e)
    {
        var picker = new FileOpenPicker();
        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow);
        WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

        picker.ViewMode = PickerViewMode.List;
        picker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
        picker.FileTypeFilter.Add(".pdf");

        var archivo = await picker.PickSingleFileAsync();
        ViewModel.RutaArchivoLocal = archivo?.Path ?? string.Empty;
    }

    // ── Drag & Drop ──
    private void DropZone_DragOver(object sender, DragEventArgs e)
    {
        e.AcceptedOperation = DataPackageOperation.Copy;
        e.DragUIOverride.Caption = "Soltar PDF aquí";
        e.DragUIOverride.IsGlyphVisible = true;
    }

    private async void DropZone_Drop(object sender, DragEventArgs e)
    {
        if (e.DataView.Contains(StandardDataFormats.StorageItems))
        {
            var items = await e.DataView.GetStorageItemsAsync();
            if (items.Count > 0 &&
                items[0] is Windows.Storage.StorageFile file &&
                file.FileType.Equals(".pdf", StringComparison.OrdinalIgnoreCase))
            {
                ViewModel.RutaArchivoLocal = file.Path;
            }
        }
    }
}