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