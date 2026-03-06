namespace DSA.Presentation.Views;

using Microsoft.UI.Xaml.Controls;
using Microsoft.Extensions.DependencyInjection;
using DSA.Presentation.ViewModels;

public sealed partial class BusquedaPage : Page
{
    public BusquedaViewModel ViewModel { get; }

    public BusquedaPage()
    {
        this.InitializeComponent();
        ViewModel = App.Services.GetRequiredService<BusquedaViewModel>();
    }

    private void OnQuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
    {
        if (ViewModel.BuscarDocumentosCommand.CanExecute(null))
        {
            ViewModel.BuscarDocumentosCommand.Execute(null);
        }
    }
}
