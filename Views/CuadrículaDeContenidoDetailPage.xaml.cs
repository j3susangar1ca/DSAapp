using CommunityToolkit.WinUI.UI.Animations;

using DSAapp.Contracts.Services;
using DSAapp.ViewModels;

using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace DSAapp.Views;

public sealed partial class CuadrículaDeContenidoDetailPage : Page
{
    public CuadrículaDeContenidoDetailViewModel ViewModel
    {
        get;
    }

    public CuadrículaDeContenidoDetailPage()
    {
        ViewModel = App.GetService<CuadrículaDeContenidoDetailViewModel>();
        InitializeComponent();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        this.RegisterElementForConnectedAnimation("animationKeyContentGrid", itemHero);
    }

    protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
    {
        base.OnNavigatingFrom(e);
        if (e.NavigationMode == NavigationMode.Back)
        {
            var navigationService = App.GetService<INavigationService>();

            if (ViewModel.Item != null)
            {
                navigationService.SetListDataItemForNextConnectedAnimation(ViewModel.Item);
            }
        }
    }
}
