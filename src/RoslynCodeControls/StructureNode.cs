using System.Collections.Generic;

namespace RoslynCodeControls
{
    public abstract class StructureNode
    {
        public List<StructureNode> Children { get; set; } = new List<StructureNode>();

        public abstract string DisplayText {
            get;
        }
    }
}