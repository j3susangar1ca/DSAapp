// 1. APLICACIÓN: DSA.Application/DTOs/DashboardStats.cs
namespace DSA.Application.DTOs;

using System.Collections.Generic;

public record DashboardStats(
    int TotalIngresados,       // D[7]
    int TotalEnProceso,        // D[8]
    int TotalArchivados,       // D[11]
    double CumplimientoSLA,    // Porcentaje de documentos sin retraso
    List<MesaPerformance> RendimientoMesas // Datos para el gráfico de barras
);

public record MesaPerformance(string MesaId, int DocumentosAsignados, string StatusColor);

// 2. APLICACIÓN: DSA.Application/Services/AnalyticsService.cs
namespace DSA.Application.Services;

using System.Threading.Tasks;
using System.Linq;
using DSA.Application.DTOs;
using dsApp.Domain.Interfaces; // Para acceder al repositorio

public sealed class AnalyticsService(IDocumentoRepository repository)
{
    public async Task<DashboardStats> GetExecutiveMetricsAsync()
    {
        // En producción, aquí se usaría Dapper para consultas masivas de alto rendimiento
        var todos = await repository.GetAllAsync();
        
        int ingresados = todos.Count(d => (d.EstadoVector & 0x80) != 0); // D[7]
        int enProceso = todos.Count(d => (d.EstadoVector & 0x100) != 0); // D[8]
        int archivados = todos.Count(d => (d.EstadoVector & 0x800) != 0); // D[11]

        // Lógica de Semáforo / Balanceo (Regla: Diferencia < 2)
        // Simulación de cálculo por mesas M1..M5
        var mesas = Enumerable.Range(1, 5).Select(i => new MesaPerformance(
            $"Mesa {i}", 
            Random.Shared.Next(10, 15), 
            "Green")).ToList();

        return new DashboardStats(ingresados, enProceso, archivados, 94.5, mesas);
    }
}