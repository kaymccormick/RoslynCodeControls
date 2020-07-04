using System.Windows.Media;
using System.Windows.Media.TextFormatting;

namespace RoslynCodeControls
{
    public class InClassName
    {
        public InClassName(RoslynCodeControl roslynCodeControl, int lineNo, int offset, double y, double x,
            LineInfo lineInfo, TextFormatter textFormatter, double paragraphWidth, FontRendering currentRendering,
            double pixelsPerDip, CustomTextSource4 customTextSource4, double maxY, double maxX, DrawingGroup d,
            DrawingContext dc, double fontSize, string fontFamilyName)
        {
            RoslynCodeControl = roslynCodeControl;
            LineNo = lineNo;
            Offset = offset;
            Y = y;
            X = x;
            LineInfo = lineInfo;
            TextFormatter = textFormatter;
            ParagraphWidth = paragraphWidth;
            CurrentRendering = currentRendering;
            PixelsPerDip = pixelsPerDip;
            CustomTextSource4 = customTextSource4;
            MaxY = maxY;
            MaxX = maxX;
            D = d;
            Dc = dc;
            FontSize = fontSize;
            FontFamilyName = fontFamilyName;
        }


        public RoslynCodeControl RoslynCodeControl { get; private set; }
        public int LineNo { get; private set; }
        public int Offset { get; private set; }
        public double Y { get; private set; }
        public double X { get; private set; }
        public LineInfo LineInfo { get; private set; }
        public TextFormatter TextFormatter { get; private set; }
        public double ParagraphWidth { get; private set; }
        public FontRendering CurrentRendering { get; set; }
        public double PixelsPerDip { get; private set; }
        public CustomTextSource4 CustomTextSource4 { get; private set; }
        public double MaxY { get; private set; }
        public double MaxX { get; private set; }
        public DrawingGroup D { get; private set; }
        public DrawingContext Dc { get; private set; }
        public string FontFamilyName { get; set; }
        public double FontSize { get; set; }
    }
}