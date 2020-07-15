namespace RoslynCodeControls
{
    public class NamespaceNode : StructureNode
    {
        private readonly string _ns;
        public override string DisplayText => "Namespace " + _ns;
        public NamespaceNode(string @namespace)
        {
            _ns = @namespace;
        }
    }
}