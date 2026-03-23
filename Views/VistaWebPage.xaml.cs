using DSAapp.ViewModels;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Web.WebView2.Core; // Asegúrate de tener este using
using System;

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

        // 1. Esperamos a que el motor interno del WebView2 esté listo
        WebView.CoreWebView2Initialized += WebView_CoreWebView2Initialized;

        // 2. Mandamos a cargar la página
        WebView.Source = new Uri("https://sii.hcg.gob.mx/intranet/intro.fwx");
    }

    private void WebView_CoreWebView2Initialized(WebView2 sender, CoreWebView2InitializedEventArgs args)
    {
        // 3. Cuando el servidor nos lance esa ventanita gris pidiendo usuario/contraseña,
        // este evento lo atrapará antes de mostrarlo en pantalla.
        WebView.CoreWebView2.BasicAuthenticationRequested += CoreWebView2_BasicAuthenticationRequested;
    }

    private void CoreWebView2_BasicAuthenticationRequested(CoreWebView2 sender, CoreWebView2BasicAuthenticationRequestedEventArgs args)
    {
        // 4. Inyectamos las credenciales silenciosamente
        args.Response.UserName = "980933";
        args.Response.Password = "Berena35";

        // Opcional: Si quieres que el evento no se vuelva a disparar (por seguridad)
        // sender.BasicAuthenticationRequested -= CoreWebView2_BasicAuthenticationRequested;
    }
}