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
    public class SyntaxTokenConverter : IValueConverter
    {
        /// <inheritdoc />
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
            {
                return null;
            }
            SyntaxToken? maybetoken = (SyntaxToken?) value;
            if (maybetoken.HasValue)
            {
                var token = maybetoken.Value;
                if ((string)parameter == "kind")
                {
                    return token.Kind().ToString();
                }
                else if ((string)parameter == "text")
                {
                    return token.Text;
                }
            }

            return null;
        }

        /// <inheritdoc />
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}