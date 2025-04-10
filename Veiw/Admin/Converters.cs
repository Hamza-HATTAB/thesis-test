using System;
using System.Windows;
using System.Windows.Data;

namespace DataGridNamespace.Admin
{
    /// <summary>
    /// مجموعة من المحولات المستخدمة في واجهة المستخدم
    /// </summary>
    public class Converters
    {
        /// <summary>
        /// محول لتحويل النص إلى حالة الرؤية
        /// </summary>
        public class StringToVisibilityConverter : IValueConverter
        {
            public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
            {
                if (value == null)
                    return Visibility.Collapsed;

                string stringValue = value.ToString();

                if (string.IsNullOrEmpty(stringValue))
                    return Visibility.Collapsed;

                return Visibility.Visible;
            }

            public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
            {
                throw new NotImplementedException();
            }
        }

        /// <summary>
        /// محول لتحويل القيمة المنطقية إلى حالة الرؤية
        /// </summary>
        public class BooleanToVisibilityConverter : IValueConverter
        {
            public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
            {
                if (value == null)
                    return Visibility.Collapsed;

                bool boolValue = (bool)value;

                if (parameter != null && parameter.ToString() == "Invert")
                {
                    boolValue = !boolValue;
                }

                return boolValue ? Visibility.Visible : Visibility.Collapsed;
            }

            public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
            {
                if (value == null)
                    return false;

                Visibility visibility = (Visibility)value;

                if (parameter != null && parameter.ToString() == "Invert")
                {
                    return visibility != Visibility.Visible;
                }

                return visibility == Visibility.Visible;
            }
        }

        /// <summary>
        /// محول لتحويل القيمة المنطقية إلى نص
        /// </summary>
        public class BooleanToStringConverter : IValueConverter
        {
            public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
            {
                if (value == null)
                    return string.Empty;

                bool boolValue = (bool)value;

                if (parameter != null)
                {
                    string[] parameters = parameter.ToString().Split('|');

                    if (parameters.Length >= 2)
                    {
                        return boolValue ? parameters[0] : parameters[1];
                    }
                }

                return boolValue ? "Yes" : "No";
            }

            public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
            {
                throw new NotImplementedException();
            }
        }

        /// <summary>
        /// محول لتحويل القيمة المنطقية إلى لون
        /// </summary>
        public class BooleanToColorConverter : IValueConverter
        {
            public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
            {
                if (value == null)
                    return null;

                bool boolValue = (bool)value;

                if (parameter != null)
                {
                    string[] parameters = parameter.ToString().Split('|');

                    if (parameters.Length >= 2)
                    {
                        string colorStr = boolValue ? parameters[0] : parameters[1];
                        return System.Windows.Media.ColorConverter.ConvertFromString(colorStr);
                    }
                }

                return boolValue ?
                    System.Windows.Media.ColorConverter.ConvertFromString("#008000") :
                    System.Windows.Media.ColorConverter.ConvertFromString("#FF0000");
            }

            public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
            {
                throw new NotImplementedException();
            }
        }
    }
}
