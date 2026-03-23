using CommunityToolkit.Mvvm.ComponentModel;
using DSAapp.Core.Models;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Storage.Streams;

namespace DSAapp.ViewModels;

public partial class ScannedPageViewModel : ObservableObject, IDisposable
{
    private readonly ScannedPage _model;
    private bool _disposed;

    [ObservableProperty] private string _extractedText = string.Empty;
    [ObservableProperty] private bool _ocrProcessed;
    [ObservableProperty] private BitmapImage _imageSource = new();

    public ScannedPage ScannedPage => _model;
    public bool IsBlank => _model.IsBlank;
    public string PageLabel => _model.PageLabel;

    public ScannedPageViewModel(ScannedPage model)
    {
        _model = model;
        // Cargar la imagen para la miniatura de forma segura
        _ = LoadImageAsync();
    }

    private async Task LoadImageAsync()
    {
        _model.ImageStream.Seek(0);
        await ImageSource.SetSourceAsync(_model.ImageStream);
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _model.Dispose();
            _disposed = true;
        }
    }
}