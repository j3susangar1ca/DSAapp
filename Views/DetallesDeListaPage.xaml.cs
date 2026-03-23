using CommunityToolkit.WinUI.UI.Controls;

using DSAapp.ViewModels;

using Microsoft.UI.Xaml.Controls;

namespace DSAapp.Views;

public sealed partial class DetallesDeListaPage : Page
{
    public DetallesDeListaViewModel ViewModel
    {
        get;
    }

    public DetallesDeListaPage()
    {
        ViewModel = App.GetService<DetallesDeListaViewModel>();
        InitializeComponent();
    }

    private void OnViewStateChanged(object sender, ListDetailsViewState e)
    {
        if (e == ListDetailsViewState.Both)
        {
            ViewModel.EnsureItemSelected();
        }
    }
}
