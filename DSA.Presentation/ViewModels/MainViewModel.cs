namespace DSA.Presentation.ViewModels;

using CommunityToolkit.Mvvm.ComponentModel;

public partial class MainViewModel : ObservableObject
{
    [ObservableProperty]
    private string _title = "SIA - Sistema de Inteligencia Archivística";
}
