using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using DSA.Presentation.ViewModels;

namespace DSA.Presentation.Views;

public sealed partial class DocumentWorkPage : Page
{
    public DocumentWorkViewModel ViewModel { get; }

    public DocumentWorkPage()
    {
        this.InitializeComponent();
        this.ViewModel = App.Services.GetRequiredService<DocumentWorkViewModel>();
    }
}
