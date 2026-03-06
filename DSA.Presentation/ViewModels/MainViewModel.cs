namespace DSA.Presentation.ViewModels;

using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DSA.Domain.Entities;
using DSA.Domain.Interfaces;

public partial class MainViewModel : ObservableObject
{
    private readonly IDocumentoRepository _repository;
    
    [ObservableProperty]
    private ObservableCollection<Documento> _bandejaEntrada = new();

    [ObservableProperty]
    private Documento? _documentoSeleccionado;

    [ObservableProperty]
    private bool _isRefreshing;

    public MainViewModel(IDocumentoRepository repository)
    {
        _repository = repository;
        // Carga inicial de la bandeja
        _ = RefreshBandejaAsync();
    }

    [RelayCommand]
    private async Task RefreshBandejaAsync()
    {
        if (IsRefreshing) return;
        
        IsRefreshing = true;
        BandejaEntrada.Clear();

        var docs = await _repository.GetAllAsync();

        // Lógica de filtrado por Vector de Estado D[11:0]
        // Filtro: (D[7] == 1) AND (D[11] == 0)
        var pendientes = docs.Where(d => d.IsIngresado && !d.IsArchivado && !d.IsRechazado);

        foreach (var doc in pendientes)
        {
            BandejaEntrada.Add(doc);
        }

        IsRefreshing = false;
    }

    /// <summary>
    /// Lógica de navegación hacia la página de trabajo.
    /// </summary>
    [RelayCommand]
    private void NavegarADetalle(Documento documento)
    {
        if (documento == null) return;
        
        // En WinUI 3, la navegación se suele orquestar a través de un Frame en MainWindow
        // Este evento debe ser capturado por la View o un NavigationService
        OnDocumentoSeleccionadoParaProceso(documento);
    }

    // Evento para comunicar la selección a la UI
    public event Action<Documento>? DocumentoSeleccionadoParaProceso;
    protected virtual void OnDocumentoSeleccionadoParaProceso(Documento doc) => DocumentoSeleccionadoParaProceso?.Invoke(doc);
}
