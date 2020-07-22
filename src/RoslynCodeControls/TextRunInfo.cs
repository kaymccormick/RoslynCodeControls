using System.Windows;
using System.Windows.Media.TextFormatting;

namespace RoslynCodeControls
{
    public class TextRunInfo
    {
        public TextRun TextRun { get; }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"{nameof(TextRun)}: {TextRun}, {nameof(Rect)}: {Rect}";
        }

        public Rect Rect { get; }

        public TextRunInfo(TextRun textRun, Rect rect)
        {
            TextRun = textRun;
            Rect = rect;
        }
    }
}