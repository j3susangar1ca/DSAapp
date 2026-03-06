namespace DSA.Presentation.Views;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using DSA.Presentation.ViewModels;

public sealed partial class CapturaView : Page
{
    public CapturaViewModel ViewModel { get; }

    public CapturaView()
    {
        this.InitializeComponent();
        ViewModel = App.RootScope.ServiceProvider.GetRequiredService<CapturaViewModel>();
    }
}