using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace RoslynCodeControls
{
    public class ConstructorNode : MethodBaseNode
    {
        public ConstructorNode(ConstructorDeclarationSyntax node) : base(node)
        {
            Node = node;
            DisplayText = node.Identifier.Text;
        }

        /// <inheritdoc />
        public override string DisplayText { get; }
    }
}