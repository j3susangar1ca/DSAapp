using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using DSA.Presentation.ViewModels;

namespace DSA.Presentation.Views;

public sealed partial class CapturaView : Page
{
    public CapturaViewModel ViewModel { get; }

    public CapturaView()
    {
        this.InitializeComponent();
        ViewModel = App.Services.GetRequiredService<CapturaViewModel>();
    }
}
