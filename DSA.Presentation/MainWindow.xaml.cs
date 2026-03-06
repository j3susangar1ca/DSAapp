using Microsoft.UI.Xaml;
using Microsoft.Extensions.DependencyInjection;
using DSA.Presentation.ViewModels;

namespace DSA.Presentation;

public sealed partial class MainWindow : Window
{
    public MainViewModel ViewModel { get; }

    public MainWindow()
    {
        this.InitializeComponent();
        ViewModel = App.Services.GetRequiredService<MainViewModel>();
    }
}
