using System.Windows;
using Microsoft.CodeAnalysis;
// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace RoslynCodeControls
{
    /// <summary>
    /// 
    /// </summary>
    public class SyntaxNodeDepth
    {
        /// <summary>
        /// 
        /// </summary>
        public Thickness Margin
        {
            get { return new Thickness(Depth * 10, 0, 0, 0); }
        }

        /// <summary>
        /// 
        /// </summary>
        public int Depth { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public SyntaxNode SyntaxNode { get; set; }
    }
}