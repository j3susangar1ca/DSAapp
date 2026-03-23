using DSAapp.ViewModels;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Web.WebView2.Core;

namespace DSAapp.Views;

public sealed partial class VistaWebPage : Page
{
    public VistaWebViewModel ViewModel
    {
        get;
    }

    public VistaWebPage()
    {
        ViewModel = App.GetService<VistaWebViewModel>();
        InitializeComponent();

        ViewModel.WebViewService.Initialize(WebView);

        // Solo nos suscribimos para inyectar la contraseña, 
        // ya NO le decimos la URL aquí porque el ViewModel se encarga de eso.
        WebView.CoreWebView2Initialized += WebView_CoreWebView2Initialized;
    }

    private void WebView_CoreWebView2Initialized(WebView2 sender, CoreWebView2InitializedEventArgs args)
    {
        // Cuando el servidor pida las credenciales, las inyectamos.
        WebView.CoreWebView2.BasicAuthenticationRequested += CoreWebView2_BasicAuthenticationRequested;
    }

    private void CoreWebView2_BasicAuthenticationRequested(CoreWebView2 sender, CoreWebView2BasicAuthenticationRequestedEventArgs args)
    {
        // Pon tus datos reales aquí
        args.Response.UserName = "980933";
        args.Response.Password = "Berena35";
    }
}