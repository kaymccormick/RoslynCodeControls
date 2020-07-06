using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace RoslynCodeControls
{
    public class PointConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return null;

            Point p = (Point)value;
            return $"({p.X:N2}, {p.Y:N2})";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}