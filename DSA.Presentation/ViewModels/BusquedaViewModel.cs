namespace DSA.Presentation.ViewModels;

using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DSA.Domain.Entities;
using DSA.Domain.Interfaces;

public partial class BusquedaViewModel(IDocumentoRepository repository) : ObservableObject
{
    [ObservableProperty]
    private string _queryBusqueda = string.Empty;

    [ObservableProperty]
    private bool _isSearching;

    [ObservableProperty]
    private ObservableCollection<Documento> _resultados = new();

    // CancellationTokenSource para manejar Debounce y cancelaciones
    private CancellationTokenSource? _searchCts;

    [RelayCommand]
    private async Task BuscarDocumentosAsync()
    {
        if (string.IsNullOrWhiteSpace(QueryBusqueda) || QueryBusqueda.Length < 3) return;

        // Cancelar búsqueda anterior si el usuario sigue escribiendo (Debounce nativo)
        _searchCts?.Cancel();
        _searchCts = new CancellationTokenSource();
        var token = _searchCts.Token;

        IsSearching = true;
        Resultados.Clear();

        try
        {
            // La búsqueda delegará al repositorio para usar pg_trgm (Full-Text) en PostgreSQL
            var docs = await repository.SearchByTextAsync(QueryBusqueda, token);
            
            foreach (var doc in docs)
            {
                if (token.IsCancellationRequested) break;
                Resultados.Add(doc);
            }
        }
        catch (OperationCanceledException)
        {
            // Búsqueda cancelada por el usuario, se ignora de forma segura
        }
        catch (Exception)
        {
            // Manejo de errores simplificado
        }
        finally
        {
            IsSearching = false;
        }
    }
}
