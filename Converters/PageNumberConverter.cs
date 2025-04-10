using System;
using System.Globalization;
using System.Windows.Data;

namespace DataGridNamespace.Converters
{
    public class PageNumberConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int pageNumber)
            {
                return $"Page {pageNumber}";
            }
            return "Page ?";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}