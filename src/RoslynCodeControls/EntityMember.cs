using Microsoft.CodeAnalysis;

namespace RoslynCodeControls
{
    public class EntityMember
    {
        public EntityMember()
        {
            MemberType = GetType().Name;
        }

        public string MemberType { get; set; }
        public string Name { get; set; }
        public string Suffix { get; set; }
        public Location Location { get; set; }
        public string CodePreview { get; set; }
        public SyntaxNode Node { get; set; }
        public string AccessibilitySymbol { get; set; }
    }
}