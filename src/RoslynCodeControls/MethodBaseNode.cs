using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace RoslynCodeControls
{
    public abstract class MethodBaseNode : StructureNode
    {
        /// <inheritdoc />
        protected MethodBaseNode(BaseMethodDeclarationSyntax baseMethod)
        {

        }
    }
}