using DSAapp.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.ApplicationModel.DataTransfer;

namespace DSAapp.Views;

public sealed partial class EscanerPage : Page
{
    public EscanerViewModel ViewModel { get; }

    public EscanerPage()
    {
        ViewModel = App.GetService<EscanerViewModel>();
        InitializeComponent();
    }

    private void CopyText_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel.SelectedPage?.ExtractedText is not { Length: > 0 } text) return;

        var package = new DataPackage();
        package.SetText(text);
        Clipboard.SetContent(package);
    }
}
