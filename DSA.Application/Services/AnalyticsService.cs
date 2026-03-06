namespace DSA.Application.Services;

using System;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;
using global::DSA.Application.DTOs;
using global::DSA.Domain.Interfaces;

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
