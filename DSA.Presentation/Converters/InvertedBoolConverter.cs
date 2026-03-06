using Microsoft.UI.Xaml.Data;
using System;

namespace DSA.Presentation.Converters
{
    public sealed class InvertedBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is bool b)
                return !b;
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            if (value is bool b)
                return !b;
            return value;
        }
    }
}
