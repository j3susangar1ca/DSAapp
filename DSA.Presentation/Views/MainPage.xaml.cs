using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using DSA.Presentation.ViewModels;

namespace DSA.Presentation.Views;

public sealed partial class MainPage : Page
{
    public MainViewModel ViewModel { get; }

    public MainPage()
    {
        this.InitializeComponent();
        this.ViewModel = App.Services.GetRequiredService<MainViewModel>();
    }
}
