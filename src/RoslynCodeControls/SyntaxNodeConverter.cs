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
    public sealed class SyntaxNodeConverter : IValueConverter
    {
        
        #region Implementation of IValueConverter
        /// <summary>
        /// 
        /// </summary>
        /// <param name="value"></param>
        /// <param name="targetType"></param>
        /// <param name="parameter"></param>
        /// <param name="culture"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public object Convert (
            object      value
            , Type        targetType
            , object      parameter
            , CultureInfo culture
        )
        {
            if ( value == null )
            {
                return null ;
            }

            
            if ( value is SyntaxNode s )
            {
                if ( parameter is SyntaxNodeInfo parm )
                {
                    switch ( parm )
                    {
                        case SyntaxNodeInfo.Ancestors :        return s.Ancestors ( ) ;
                        case SyntaxNodeInfo.AncestorsAndSelf : return s.AncestorsAndSelf ( ) ;
                        case SyntaxNodeInfo.GetFirstToken :    return s.GetFirstToken ( ) ;
                        case SyntaxNodeInfo.GetLocation :      return s.GetLocation ( ) ;
                        case SyntaxNodeInfo.GetLastToken :     return s.GetLastToken ( ) ;
                        case SyntaxNodeInfo.GetReference :     return s.GetReference ( ) ;
                        case SyntaxNodeInfo.GetText :          return s.GetText ( ) ;
                        case SyntaxNodeInfo.ToFullString :     return s.ToFullString ( ) ;
                        case SyntaxNodeInfo.ToString :         return s.ToString ( ) ;
                        case SyntaxNodeInfo.Kind :
                            if (s is CSharpSyntaxNode csn)
                            {
                                return csn.Kind();
                            }
                            // else if (s is VisualBasicSyntaxNode vbn)
                                // return vbn.Kind();
                            return null;
                        case SyntaxNodeInfo.ChildNodesAndTokens : return s.ChildNodesAndTokens ( ) ;
                        case SyntaxNodeInfo.ChildNodes :          return s.ChildNodes ( ) ;
                        case SyntaxNodeInfo.ChildTokens :         return s.ChildTokens ( ) ;
                        case SyntaxNodeInfo.DescendantNodes :     return s.DescendantNodes ( ) ;
                        case SyntaxNodeInfo.DescendantNodesAndSelf :
                            return s.DescendantNodesAndSelf ( ) ;
                        case SyntaxNodeInfo.DescendantNodesAndTokens :
                            return s.DescendantNodesAndTokens ( ) ;
                        case SyntaxNodeInfo.DescendantNodesAndTokensAndSelf :
                            return s.DescendantNodesAndTokensAndSelf ( ) ;
                        case SyntaxNodeInfo.DescendantTokens :
                            return s.DescendantTokens ( node => true , true ) ;
                        case SyntaxNodeInfo.Diagnostics :      return s.GetDiagnostics ( ) ;
                        case SyntaxNodeInfo.DescendantTrivia : return s.DescendantTrivia ( ) ;
                        case SyntaxNodeInfo.GetLeadingTrivia : return s.GetLeadingTrivia ( ) ;
                        default :
                            throw new ArgumentOutOfRangeException ( ) ;
                    }
                }

                // Logger.Debug ( "returning null for {s} {t}" , s , targetType.FullName ) ;
                return null ;
            }

            // Logger.Debug ( "returning null for {s} {t}" , value , targetType.FullName ) ;
            return null ;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="value"></param>
        /// <param name="targetType"></param>
        /// <param name="parameter"></param>
        /// <param name="culture"></param>
        /// <returns></returns>
        public object ConvertBack (
            object      value
            , Type        targetType
            , object      parameter
            , CultureInfo culture
        )
        {
            if (value != null)
            {
                var convertBack = Convert(value, targetType, parameter, culture);
                if (targetType == typeof(string))
                {
                    return convertBack?.ToString() ?? "";
                }
                return convertBack;
            }
            return null ;
        }
        #endregion
    }
}