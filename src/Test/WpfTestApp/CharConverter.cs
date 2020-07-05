using System;
using System.Globalization;
using System.Windows.Data;

namespace WpfTestApp
{
    public class CharConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return null;
            var ch = (char) value;
            if (ch == ' ')
            {
                return '\u2420';
            }

            return ch;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class CharFontSizeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return null;
            var ch = (char)value;
            if (ch == ' ')
            {
                return 28.0;
            }

            return 24.0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
    