using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace RoslynCodeControls
{
    public abstract class StructureNode
    {
        public List<StructureNode> Children { get; set; } = new List<StructureNode>();
        public SyntaxNode Node { get; set; } = null!; 

        public abstract string DisplayText {
            get;
        }
    }
}