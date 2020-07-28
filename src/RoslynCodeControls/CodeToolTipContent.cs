using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.QuickInfo;

// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace RoslynCodeControls
{
    /// <summary>
    /// 
    /// </summary>
    public class CodeToolTipContent
    {
        /// <summary>
        /// 
        /// </summary>
        public CodeToolTipContent()
        {
        }

        public QuickInfoItem QuickInfoItem { get; set; }
        public SyntaxNode SyntaxNode { get; set; }
        public ISymbol Symbol { get; set; }
        public IEnumerable<SyntaxNodeDepth> Nodes { get; set; }
        public IOperation Operation { get; set; }
    }
}