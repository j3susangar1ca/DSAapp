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
    private ObservableCollection<Tarea> _tareas = new();

    public TareasViewModel(AppDbContext db)
    {
        _db = db;
    }

    [RelayCommand]
    public async Task CargarTareasAsync()
    {
        var lista = await _db.Tareas.ToListAsync();
        Tareas.Clear();
        foreach (var t in lista) Tareas.Add(t);
    }

    [RelayCommand]
    public async Task AgregarTareaPruebaAsync()
    {
        var nueva = new Tarea
        {
            Titulo = "Primera Tarea en Red",
            Descripcion = "Prueba de escritura desde WinUI 3"
        };

        _db.Tareas.Add(nueva);
        await _db.SaveChangesAsync();
        await CargarTareasAsync();
    }
}