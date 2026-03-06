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
        try 
        {
            BandejaEntrada.Clear();
            var docs = await _repository.GetAllAsync();

            // Filtro de estados no terminales basado en el Vector de Estado
            // Lógica: !(D[11] ARCH) AND !(D[10] RECH)
            var activos = docs.Where(d => !d.IsArchivado && !d.IsRechazado);

            foreach (var doc in activos)
            {
                BandejaEntrada.Add(doc);
            }
        }
        finally 
        {
            IsRefreshing = false;
        }
    }

    /// <summary>
    /// Comando de Selección de Trámite para navegación.
    /// </summary>
    [RelayCommand]
    private void SeleccionarDocumento(Documento documento)
    {
        if (documento == null) return;
        
        DocumentoSeleccionado = documento;
        
        // Dispara el evento de navegación que debe ser capturado en MainWindow
        OnNavegacionRequerida(documento);
    }

    // Evento para comunicar la intención de navegación a la View/MainWindow
    public event Action<Documento>? NavegacionRequerida;
    protected virtual void OnNavegacionRequerida(Documento doc) => NavegacionRequerida?.Invoke(doc);
}
