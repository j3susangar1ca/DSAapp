using DSAapp.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.ApplicationModel.DataTransfer;

namespace DSAapp.Views;

public sealed partial class EscanerPage : Page
{
    public EscanerViewModel ViewModel
    {
        get;
    }

    public EscanerPage()
    {
        ViewModel = App.GetService<EscanerViewModel>();
        InitializeComponent();
    }

    private void CopyText_Click(object sender, RoutedEventArgs e)
    {
        var dataPackage = new DataPackage();
        dataPackage.SetText(ViewModel.SelectedPage?.ExtractedText ?? "");
        Clipboard.SetContent(dataPackage);
    }
}