using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace WpfTestApp
{
    public class VisibilityToBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
            {
                return null;
            }

            var v = (Visibility)value;
            return v == Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {

            if (value == null)
            {
                return null;
            }

            var b = (bool)value;
            return b ? Visibility.Visible : Visibility.Collapsed;
        }
    }
}