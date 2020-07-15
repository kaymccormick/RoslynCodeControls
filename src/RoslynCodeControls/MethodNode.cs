namespace RoslynCodeControls
{
    public class MethodNode : StructureNode
    {
        private string _displayText;
        public override string DisplayText => _displayText;

        public MethodNode(string displayText)
        {
            _displayText = displayText;
        }
    }
}