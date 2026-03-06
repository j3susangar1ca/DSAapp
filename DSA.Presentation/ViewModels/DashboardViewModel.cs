namespace DSA.Presentation.ViewModels;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using DSA.Application.DTOs;
using DSA.Application.Services;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI;

public partial class DashboardViewModel : ObservableObject
{
    private readonly AnalyticsService _analyticsService;

    [ObservableProperty]
    private ObservableCollection<ISeries> _series = [];

    [ObservableProperty]
    private SolidColorBrush _systemHealthColor = new(Colors.Green);

    [ObservableProperty]
    private string _healthMessage = "Sistema Operativo";

    [ObservableProperty]
    private bool _hasAlert;

    public DashboardViewModel(AnalyticsService analyticsService)
    {
        _analyticsService = analyticsService;
        _ = LoadDataAsync();
    }

    private async Task LoadDataAsync()
    {
        var stats = await _analyticsService.GetExecutiveMetricsAsync();

        // Configuración de Series para LiveCharts2
        Series = new ObservableCollection<ISeries>
        {
            new ColumnSeries<int>
            {
                Values = stats.RendimientoMesas.Select(m => m.DocumentosAsignados).ToArray(),
                Name = "Documentos por Mesa",
                Fill = new SolidColorPaint(SKColors.CornflowerBlue)
            }
        };

        // Lógica de salud del sistema
        if (stats.CumplimientoSLA < 90)
        {
            SystemHealthColor = new SolidColorBrush(Colors.Red);
            HealthMessage = "Crítico - SLA Bajo";
            HasAlert = true;
        }
        else if (stats.CumplimientoSLA < 95)
        {
            SystemHealthColor = new SolidColorBrush(Colors.Orange);
            HealthMessage = "Atención Requerida";
            HasAlert = true;
        }
        else
        {
            SystemHealthColor = new SolidColorBrush(Colors.Green);
            HealthMessage = "Sistema Operativo";
            HasAlert = false;
        }
    }
}
