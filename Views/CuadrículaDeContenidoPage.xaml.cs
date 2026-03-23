using DSAapp.ViewModels;

using Microsoft.UI.Xaml.Controls;

namespace DSAapp.Views;

public sealed partial class CuadrículaDeContenidoPage : Page
{
    public CuadrículaDeContenidoViewModel ViewModel
    {
        get;
    }

    public CuadrículaDeContenidoPage()
    {
        ViewModel = App.GetService<CuadrículaDeContenidoViewModel>();
        InitializeComponent();
    }
}
