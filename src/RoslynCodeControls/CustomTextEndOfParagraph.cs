using System.Windows.Media.TextFormatting;
// ReSharper disable UnusedAutoPropertyAccessor.Global

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