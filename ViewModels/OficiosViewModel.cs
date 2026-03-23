using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DSAapp.Core.Models;
using DSAapp.Core.Services;
using Microsoft.EntityFrameworkCore;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace DSAapp.ViewModels;

public partial class OficiosViewModel : ObservableObject
{
    private readonly AppDbContext _db;

    // Propiedades enlazadas a las cajas de texto de la pantalla (UI)
    [ObservableProperty] private string _remitente = string.Empty;
    [ObservableProperty] private string _asunto = string.Empty;
    [ObservableProperty] private string _usuarioAsignado = string.Empty;
    [ObservableProperty] private string _folioOriginal = string.Empty;

    // Aquí guardaremos temporalmente la ruta del PDF que el usuario seleccione en su PC
    [ObservableProperty] private string _rutaArchivoLocal = string.Empty;

    public OficiosViewModel(AppDbContext db)
    {
        _db = db;
    }

    [RelayCommand]
    public async Task GuardarOficioAsync()
    {
        // Validaciones básicas antes de intentar guardar
        if (string.IsNullOrWhiteSpace(Remitente) || string.IsNullOrWhiteSpace(Asunto) || string.IsNullOrWhiteSpace(UsuarioAsignado))
        {
            System.Diagnostics.Debug.WriteLine("Faltan campos obligatorios por llenar.");
            return; // Detiene el proceso si falta información
        }

        try
        {
            // 1. GENERAR EL FOLIO CONSECUTIVO (Ej: OF-2026-0001)
            string añoActual = DateTime.Now.Year.ToString();

            // Cuenta cuántos oficios hay de este año para sacar el siguiente número
            int cantidadActual = await _db.Oficios
                .Where(o => o.FolioInterno.Contains(añoActual))
                .CountAsync();

            // :D4 rellena con ceros a la izquierda (ej. 1 se vuelve 0001)
            string nuevoFolio = $"OF-{añoActual}-{(cantidadActual + 1):D4}";

            // 2. PREPARAR LA RUTA EN EL SERVIDOR DE RED
            // Agregamos una subcarpeta "OficiosPDF" para no mezclar los PDFs con el archivo .db
            string rutaServidorBase = @"\\10.2.1.92\FAA_divserv_admvos\APLICACIONES\GestionProyectos\OficiosPDF";
            string nombreArchivo = $"{nuevoFolio}.pdf";
            string rutaDestinoRed = Path.Combine(rutaServidorBase, nombreArchivo);

            // Si la carpeta "OficiosPDF" no existe en el servidor, la creamos
            if (!Directory.Exists(rutaServidorBase))
            {
                Directory.CreateDirectory(rutaServidorBase);
            }

            // 3. COPIAR EL PDF ESCANEADO AL SERVIDOR
            if (!string.IsNullOrEmpty(RutaArchivoLocal) && File.Exists(RutaArchivoLocal))
            {
                // Copia el archivo local a la red y reemplaza si por alguna razón ya existía
                File.Copy(RutaArchivoLocal, rutaDestinoRed, overwrite: true);
            }
            else
            {
                rutaDestinoRed = "Sin archivo adjunto";
            }

            // 4. CREAR EL REGISTRO PARA LA BASE DE DATOS
            var nuevoOficio = new Oficio
            {
                FolioInterno = nuevoFolio,
                FolioOriginal = this.FolioOriginal,
                Remitente = this.Remitente,
                Asunto = this.Asunto,
                UsuarioAsignado = this.UsuarioAsignado,
                RutaArchivoRed = rutaDestinoRed
            };

            // 5. GUARDAR LOS CAMBIOS
            _db.Oficios.Add(nuevoOficio);
            await _db.SaveChangesAsync();

            System.Diagnostics.Debug.WriteLine($"¡Éxito! Oficio {nuevoFolio} guardado en red.");

            // Limpiar el formulario para capturar el siguiente
            LimpiarFormulario();
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