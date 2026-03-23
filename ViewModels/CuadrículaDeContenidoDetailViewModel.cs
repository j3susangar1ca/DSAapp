using CommunityToolkit.Mvvm.ComponentModel;

using DSAapp.Contracts.ViewModels;
using DSAapp.Core.Contracts.Services;
using DSAapp.Core.Models;

namespace DSAapp.ViewModels;

public partial class CuadrículaDeContenidoDetailViewModel : ObservableRecipient, INavigationAware
{
    private readonly ISampleDataService _sampleDataService;

    [ObservableProperty]
    private SampleOrder? item;

    public CuadrículaDeContenidoDetailViewModel(ISampleDataService sampleDataService)
    {
        _sampleDataService = sampleDataService;
    }

    public async void OnNavigatedTo(object parameter)
    {
        if (parameter is long orderID)
        {
            var data = await _sampleDataService.GetContentGridDataAsync();
            Item = data.First(i => i.OrderID == orderID);
        }
    }

    public void OnNavigatedFrom()
    {
    }
}
