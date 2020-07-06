using System.Windows.Media.TextFormatting;

namespace RoslynCodeControls
{
    public class CustomTextEndOfParagraph : TextEndOfParagraph
    {
        public int? Index { get; set; }

        public CustomTextEndOfParagraph(int i):base(i)
        {
        }
    }
}