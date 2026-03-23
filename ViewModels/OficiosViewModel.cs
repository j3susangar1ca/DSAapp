using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DSAapp.Core.Models;
using DSAapp.Core.Services;
using Microsoft.EntityFrameworkCore;
using System.IO;

namespace DSAapp.ViewModels;

public partial class OficiosViewModel : ObservableObject
{
    private readonly AppDbContext _db;

    // ── Campos del formulario ──
    [ObservableProperty] private string _remitente = string.Empty;
    [ObservableProperty] private string _asunto = string.Empty;
    [ObservableProperty] private string _usuarioAsignado = string.Empty;
    [ObservableProperty] private string _folioOriginal = string.Empty;

    // ── Archivo adjunto ──
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TieneArchivoSeleccionado))]
    [NotifyPropertyChangedFor(nameof(NombreArchivoSeleccionado))]
    private string _rutaArchivoLocal = string.Empty;

    // ── Feedback: último folio generado ──
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TieneUltimoFolio))]
    private string _ultimoFolioGenerado = string.Empty;

    // ── Propiedades calculadas para los bindings de la vista ──
    public bool TieneArchivoSeleccionado => !string.IsNullOrEmpty(RutaArchivoLocal);
    public string NombreArchivoSeleccionado => Path.GetFileName(RutaArchivoLocal);
    public bool TieneUltimoFolio => !string.IsNullOrEmpty(UltimoFolioGenerado);

    public OficiosViewModel(AppDbContext db) => _db = db;

    [RelayCommand]
    public async Task GuardarOficioAsync()
    {
        if (string.IsNullOrWhiteSpace(Remitente) ||
            string.IsNullOrWhiteSpace(Asunto) ||
            string.IsNullOrWhiteSpace(UsuarioAsignado))
        {
            System.Diagnostics.Debug.WriteLine("Faltan campos obligatorios.");
            return;
        }

        try
        {
            // 1. Generar folio consecutivo
            var añoActual = DateTime.Now.Year.ToString();
            var cantidadActual = await _db.Oficios
                                          .Where(o => o.FolioInterno.Contains(añoActual))
                                          .CountAsync();
            var nuevoFolio = $"OF-{añoActual}-{(cantidadActual + 1):D4}";

            // 2. Preparar ruta en servidor de red
            var rutaServidorBase = @"\\10.2.1.92\FAA_divserv_admvos\APLICACIONES\GestionProyectos\OficiosPDF";
            var nombreArchivo = $"{nuevoFolio}.pdf";
            var rutaDestinoRed = Path.Combine(rutaServidorBase, nombreArchivo);

            if (!Directory.Exists(rutaServidorBase))
                Directory.CreateDirectory(rutaServidorBase);

            // 3. Copiar PDF al servidor
            if (!string.IsNullOrEmpty(RutaArchivoLocal) && File.Exists(RutaArchivoLocal))
                File.Copy(RutaArchivoLocal, rutaDestinoRed, overwrite: true);
            else
                rutaDestinoRed = "Sin archivo adjunto";

            // 4. Persistir en base de datos
            var nuevoOficio = new Oficio
            {
                FolioInterno = nuevoFolio,
                FolioOriginal = FolioOriginal,
                Remitente = Remitente,
                Asunto = Asunto,
                UsuarioAsignado = UsuarioAsignado,
                RutaArchivoRed = rutaDestinoRed
            };

            _db.Oficios.Add(nuevoOficio);
            await _db.SaveChangesAsync();

            // 5. Feedback visual + limpiar formulario
            UltimoFolioGenerado = nuevoFolio;
            LimpiarFormulario();

            System.Diagnostics.Debug.WriteLine($"Oficio {nuevoFolio} guardado correctamente.");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error al guardar el oficio: {ex.Message}");
        }
    }

    private void LimpiarFormulario()
    {
        Remitente = string.Empty;
        Asunto = string.Empty;
        UsuarioAsignado = string.Empty;
        FolioOriginal = string.Empty;
        RutaArchivoLocal = string.Empty;
    }
}