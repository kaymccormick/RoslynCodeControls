using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace RoslynCodeControls
{
    public class RectConverter : IValueConverter
    {
        /// <inheritdoc />
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return null;

            Rect r = (Rect)value;
            if (r.IsEmpty)
            {
                return "Empty";
            }
            var x = (int)r.X;
            var y = (int)r.Y;
            var right = (int)r.Width;
            var bottom = (int)r.Bottom;
            return $"( {x}, {y} ) - ( {right}, {bottom} )";
        }

        /// <inheritdoc />
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

