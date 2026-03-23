using System.Collections.ObjectModel;

using CommunityToolkit.Mvvm.ComponentModel;

using DSAapp.Contracts.ViewModels;
using DSAapp.Core.Contracts.Services;
using DSAapp.Core.Models;

namespace DSAapp.ViewModels;

public partial class CuadrículaDeDatosViewModel : ObservableRecipient, INavigationAware
{
    private readonly ISampleDataService _sampleDataService;

    public ObservableCollection<SampleOrder> Source { get; } = new ObservableCollection<SampleOrder>();

    public CuadrículaDeDatosViewModel(ISampleDataService sampleDataService)
    {
        _sampleDataService = sampleDataService;
    }

    public async void OnNavigatedTo(object parameter)
    {
        Source.Clear();

        // TODO: Replace with real data.
        var data = await _sampleDataService.GetGridDataAsync();

        foreach (var item in data)
        {
            Source.Add(item);
        }
    }

    public void OnNavigatedFrom()
    {
    }
}
