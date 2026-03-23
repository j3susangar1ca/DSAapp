using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DSAapp.Core.Models;
using DSAapp.Core.Services;
using Microsoft.EntityFrameworkCore;

namespace DSAapp.ViewModels;

public partial class TareasViewModel : ObservableObject
{
    private readonly AppDbContext _db;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TotalTareas))]
    [NotifyPropertyChangedFor(nameof(TareasCompletadas))]
    [NotifyPropertyChangedFor(nameof(TareasPendientes))]
    private ObservableCollection<Tarea> _tareas = new();

    [ObservableProperty]
    private bool _isBusy;

    // ── Propiedades computadas para las tarjetas del dashboard ──
    public int TotalTareas => Tareas.Count;
    public int TareasCompletadas => Tareas.Count(t => t.EstaCompletada);
    public int TareasPendientes => Tareas.Count(t => !t.EstaCompletada);

    public TareasViewModel(AppDbContext db) => _db = db;

    [RelayCommand]
    public async Task CargarTareasAsync()
    {
        IsBusy = true;
        try
        {
            var lista = await _db.Tareas
                                 .OrderByDescending(t => t.FechaCreacion)
                                 .ToListAsync();
            Tareas.Clear();
            foreach (var t in lista) Tareas.Add(t);

            OnPropertyChanged(nameof(TotalTareas));
            OnPropertyChanged(nameof(TareasCompletadas));
            OnPropertyChanged(nameof(TareasPendientes));
        }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    public async Task AgregarTareaPruebaAsync()
    {
        var nueva = new Tarea
        {
            Titulo = "Nueva Tarea",
            Descripcion = "Generada desde el Panel Institucional"
        };
        _db.Tareas.Add(nueva);
        await _db.SaveChangesAsync();
        await CargarTareasAsync();
    }
}