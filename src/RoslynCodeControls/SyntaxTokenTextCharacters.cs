using System;
using System.Windows.Media.TextFormatting;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace RoslynCodeControls
{
    /// <inheritdoc />
    public class SyntaxTokenTextCharacters : CustomTextCharacters
    {
        public SyntaxToken Token { get; }
        public SyntaxNode Node { get; }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"SyntaxToken.{Token.Kind()} [{Node.Kind()}] [{Length}]";
        }

        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="characterString"></param>
        /// <param name="length"></param>
        /// <param name="textRunProperties"></param>
        /// <param name="token"></param>
        /// <param name="node"></param>
        /// <exception cref="InvalidOperationException"></exception>
        public SyntaxTokenTextCharacters(string characterString, int length,
            TextRunProperties textRunProperties, SyntaxToken token, SyntaxNode node) : base(characterString, 0, length, textRunProperties, token.Span)
        {
            Token = token;
#if SUPPORTVB
            Microsoft.CodeAnalysis.VisualBasic.SyntaxKind kind;
#endif
            SyntaxKind csKind;
            switch (token.Language)
            {
#if SUPPORTVB
                case LanguageNames.VisualBasic:
                    kind = VisualBasicExtensions.Kind(token);
                    if (kind == Microsoft.CodeAnalysis.VisualBasic.SyntaxKind.None)
                    {
                        throw new AppInvalidOperationException(token.Span.ToString());
                    }
                    break;
#endif
                case LanguageNames.CSharp:
                    csKind = Token.Kind();
                    if(csKind == SyntaxKind.None)
                        throw new InvalidOperationException(token.Span.ToString());
                    break;
            }

            Node = node;
        }
    }
}