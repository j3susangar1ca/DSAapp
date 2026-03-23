using CommunityToolkit.Mvvm.ComponentModel;

using DSAapp.Contracts.Services;
using DSAapp.ViewModels;
using DSAapp.Views;

using Microsoft.UI.Xaml.Controls;

namespace DSAapp.Services;

public class PageService : IPageService
{
    private readonly Dictionary<string, Type> _pages = new();

    public PageService()
    {
        Configure<MainViewModel, MainPage>();
        Configure<VistaWebViewModel, VistaWebPage>();
        Configure<CuadrículaDeDatosViewModel, CuadrículaDeDatosPage>();
        Configure<CuadrículaDeContenidoViewModel, CuadrículaDeContenidoPage>();
        Configure<CuadrículaDeContenidoDetailViewModel, CuadrículaDeContenidoDetailPage>();
        Configure<DetallesDeListaViewModel, DetallesDeListaPage>();
        Configure<SettingsViewModel, SettingsPage>();
            Configure<OficiosViewModel, OficiosPage>();
            Configure<EscanerViewModel, EscanerPage>();
    }

    public Type GetPageType(string key)
    {
        Type? pageType;
        lock (_pages)
        {
            if (!_pages.TryGetValue(key, out pageType))
            {
                throw new ArgumentException($"Page not found: {key}. Did you forget to call PageService.Configure?");
            }
        }

        return pageType;
    }

    private void Configure<VM, V>()
        where VM : ObservableObject
        where V : Page
    {
        lock (_pages)
        {
            var key = typeof(VM).FullName!;
            if (_pages.ContainsKey(key))
            {
                throw new ArgumentException($"The key {key} is already configured in PageService");
            }

            var type = typeof(V);
            if (_pages.ContainsValue(type))
            {
                throw new ArgumentException($"This type is already configured with key {_pages.First(p => p.Value == type).Key}");
            }

            _pages.Add(key, type);
        }
    }
}
