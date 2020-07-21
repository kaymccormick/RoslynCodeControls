using System;
using System.Globalization;
using System.Linq;
using System.Windows.Data;
using Microsoft.CodeAnalysis;

namespace RoslynCodeControls
{
    /// <summary>
    /// 
    /// </summary>
    public class SymbolInfoConverter : IValueConverter
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="value"></param>
        /// <param name="targetType"></param>
        /// <param name="parameter"></param>
        /// <param name="culture"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            ISymbol symbol = (ISymbol) value;
            if (value == null)
            {
                return null;
            }

            if ((string) parameter == "kind")
            {

                return symbol.Kind.ToString();
            }
            if((string)parameter == "Members")
            {
                if (symbol is INamespaceOrTypeSymbol torn)
                {
                    return torn.GetMembers();
                }

                return Enumerable.Empty<object>();
            } else if((string)parameter == "GenericTypeDefinition")
            {
                if (symbol is INamedTypeSymbol s) return s.OriginalDefinition;
            }

            return symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="value"></param>
        /// <param name="targetType"></param>
        /// <param name="parameter"></param>
        /// <param name="culture"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}