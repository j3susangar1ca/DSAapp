using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

namespace DSAapp.Helpers;

/// <summary>
/// Convierte bool → Visibility (true = Visible, false = Collapsed)
/// </summary>
public class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
        => value is true ? Visibility.Visible : Visibility.Collapsed;

    public object ConvertBack(object value, Type targetType, object parameter, string language)
        => value is Visibility.Visible;
}

/// <summary>
/// Convierte bool → Visibility inverso (true = Collapsed, false = Visible)
/// Útil para estados vacíos: sólo muestra cuando IsBusy es false y Tareas.Count == 0.
/// </summary>
public class BoolToInverseVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
        => value is true ? Visibility.Collapsed : Visibility.Visible;

    public object ConvertBack(object value, Type targetType, object parameter, string language)
        => value is Visibility.Collapsed;
}

/// <summary>
/// Formatea DateTime a "dd/MM/yyyy HH:mm"
/// </summary>
public class DateFormatConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
        => value is DateTime dt ? dt.ToString("dd/MM/yyyy HH:mm") : string.Empty;

    public object ConvertBack(object value, Type targetType, object parameter, string language)
        => throw new NotImplementedException();
}