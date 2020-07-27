using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace RoslynCodeControls
{
    public class PropertyNode : StructureNode
    {
        public PropertyNode(PropertyDeclarationSyntax node)
        {
            Node = node;
            DisplayText = node.Identifier.Text;
        }

        /// <inheritdoc />
        public override string DisplayText { get; }
    }
}