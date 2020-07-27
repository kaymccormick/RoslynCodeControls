using System;
using System.Globalization;
using System.Windows.Data;
using Microsoft.CodeAnalysis;

namespace RoslynCodeControls
{
    public class LocationConverter: IValueConverter
    {
        /// <inheritdoc />
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return null;
            var l = (Location) value;
            var startLinePosition = l.GetLineSpan().StartLinePosition;
            var line = startLinePosition.Line+1;
            return $"{line}:{startLinePosition.Character+1}";
        }

        /// <inheritdoc />
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }
}