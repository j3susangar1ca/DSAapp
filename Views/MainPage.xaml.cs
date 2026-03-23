using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using DSAapp.ViewModels;

namespace DSAapp.Views;

public sealed partial class MainPage : Page
{
    public TareasViewModel ViewModel
    {
        get;
    }

    public MainPage()
    {
        ViewModel = App.GetService<TareasViewModel>();
        InitializeComponent();
        Loaded += MainPage_Loaded;
    }

    private async void MainPage_Loaded(object sender, RoutedEventArgs e)
        => await ViewModel.CargarTareasAsync();
}