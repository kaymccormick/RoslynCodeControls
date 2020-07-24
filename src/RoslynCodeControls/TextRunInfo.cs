using System.Collections.Generic;
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
        public LinkedListNode<CharInfo> StartCharInfo { get; }

        public TextRunInfo(TextRun textRun, Rect rect, LinkedListNode<CharInfo> startCharInfo)
        {
            TextRun = textRun;
            Rect = rect;
            StartCharInfo = startCharInfo;
        }
    }
}