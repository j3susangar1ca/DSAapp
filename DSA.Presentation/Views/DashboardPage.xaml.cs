using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using DSA.Presentation.ViewModels;

namespace DSA.Presentation.Views;

public sealed partial class DashboardPage : Page
{
    public DashboardViewModel ViewModel { get; }

    public DashboardPage()
    {
        this.InitializeComponent();
        this.ViewModel = App.RootScope.ServiceProvider.GetRequiredService<DashboardViewModel>();
    }
}
