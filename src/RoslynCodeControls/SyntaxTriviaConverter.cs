using System;
using System.Globalization;
using System.Windows.Data;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace RoslynCodeControls
{
    /// <summary>
    /// 
    /// </summary>
    public class SyntaxTriviaConverter : IValueConverter
    {
        /// <inheritdoc />
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            SyntaxTrivia? v = (SyntaxTrivia?) value;
            if (value == null || !v.HasValue)
                return null;
            var s = v.Value;
            return s.Kind().ToString();
        }

        /// <inheritdoc />
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}