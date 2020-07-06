using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using CSharpExtensions = Microsoft.CodeAnalysis.CSharp.CSharpExtensions;

namespace RoslynCodeControls
{
    internal class SyntaxInfo
    {
        public SyntaxTrivia? SyntaxTrivia { get; }
        public SyntaxToken? Token { get; }
        public TriviaPosition? TriviaPosition { get; }
        public SyntaxToken? SyntaxToken { get; }

        public SyntaxInfo(in SyntaxTrivia syntaxTrivia, SyntaxToken token, TriviaPosition triviaPosition)
        {
            SyntaxTrivia = syntaxTrivia;
            Token = token;
            TriviaPosition = triviaPosition;
            Span1 = syntaxTrivia.FullSpan;
            Text = syntaxTrivia.ToFullString();
        }

        public TextSpan Span1 { get; set; }

        public SyntaxInfo(in SyntaxToken syntaxToken)
        {
            SyntaxToken = syntaxToken;
            Span1 = syntaxToken.Span;
            Text = syntaxToken.Text;
        }

        public SyntaxInfo(in SyntaxTrivia syntaxTrivia, SyntaxNode node)
        {
            SyntaxTrivia = syntaxTrivia;
            Node = node;
            Span1 = syntaxTrivia.FullSpan;
            Text = syntaxTrivia.ToFullString();
        }

        public SyntaxNode Node { get; set; }

        public string Text { get; set; }
        public SyntaxNode StructuredTrivia { get; set; }

        public override string ToString()
        {
            return $"{Span1} " + (SyntaxTrivia.HasValue
                ? "SyntaxTrivia " + CSharpExtensions.Kind(SyntaxTrivia.Value)
                : "SyntaxToken " + CSharpExtensions.Kind(SyntaxToken.Value)) + " " + Text;
        }
    }
}