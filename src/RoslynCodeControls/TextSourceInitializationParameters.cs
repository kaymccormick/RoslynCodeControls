using System.Windows.Media;
using System.Windows.Threading;
using Microsoft.CodeAnalysis;

namespace RoslynCodeControls
{
    public class ParametersBase
    {
        public double PixelsPerDip { get; set; }
        public double EmSize0 { get; set; }
    }

    public class TextSourceInitializationParameters : ParametersBase
    {
        public TextSourceInitializationParameters(double pixelsPerDip, double emSize0,
            SyntaxTree tree, SyntaxNode node0, Compilation compilation, Typeface tf)
        {
            
            PixelsPerDip = pixelsPerDip;
            EmSize0 = emSize0;
            Tree = tree;
            Node0 = node0;
            Compilation = compilation;
            Tf = tf;
            
        }

        public RoslynCodeControl RoslynCodeControl { get; private set; }
        public SyntaxTree Tree { get; private set; }
        public SyntaxNode Node0 { get; private set; }
        public Compilation Compilation { get; private set; }
        public Typeface Tf { get; private set; }
        
    }
}