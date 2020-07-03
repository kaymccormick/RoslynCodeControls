using System.Windows.Media.TextFormatting;
using JetBrains.Annotations;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;

namespace RoslynCodeControls
{
    internal class SyntaxTriviaTextCharacters : CustomTextCharacters
    {
        public SyntaxTrivia Trivia { get; }
        public SyntaxNode Node { get; }
        public SyntaxToken? Token { get; }
        public TriviaPosition? TriviaPosition { get; }
        public SyntaxNode StructuredTrivia { get; }


        public SyntaxTriviaTextCharacters([NotNull] string characterString,
            [NotNull] TextRunProperties textRunProperties, TextSpan span, SyntaxTrivia syntaxTrivia, SyntaxNode node,
            SyntaxToken? token, TriviaPosition? triviaPosition, SyntaxNode structuredTrivia = null) : base(characterString, textRunProperties, span)
        {
            Trivia = syntaxTrivia;
            Node = node;
            Token = token;
            TriviaPosition = triviaPosition;
            StructuredTrivia = structuredTrivia;
        }

        public SyntaxTriviaTextCharacters([NotNull] string characterString, int offsetToFirstChar, int length, [NotNull] TextRunProperties textRunProperties, TextSpan span, SyntaxTrivia syntaxTrivia) : base(characterString, offsetToFirstChar, length, textRunProperties, span)
        {
            Trivia = syntaxTrivia;
        }

        public override string ToString()
        {
            return $"SyntaxTrivia {Trivia.Kind()} [{Length}]";
        }

    }
}