using System.Windows;
using System.Windows.Media;
using System.Windows.Media.TextFormatting;
// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace RoslynCodeControls
{
    public struct CallbackParameters1
    {
        public CallbackParameters1(RoslynCodeControl roslynCodeControl, int lineNo, int offset, double y, double x,
            LineInfo2 lineInfo, TextFormatter textFormatter, double paragraphWidth, FontRendering currentRendering,
            double pixelsPerDip, CustomTextSource4 customTextSource4, double maxY, double maxX, double fontSize,
            string fontFamilyName, FontWeight fontWeight)
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
            FontSize = fontSize;
            FontFamilyName = fontFamilyName;
            FontWeight = fontWeight;
        }


        public RoslynCodeControl RoslynCodeControl { get; private set; }
        public int LineNo { get; private set; }
        public int Offset { get; private set; }
        public double Y { get; private set; }
        public double X { get; private set; }
        public LineInfo2 LineInfo { get; private set; }
        public TextFormatter TextFormatter { get; private set; }
        public double ParagraphWidth { get; private set; }
        public FontRendering CurrentRendering { get; set; }
        public double PixelsPerDip { get; private set; }
        public CustomTextSource4 CustomTextSource4 { get; private set; }
        public double MaxY { get; private set; }
        public double MaxX { get; private set; }
        public string FontFamilyName { get; set; }
        public double FontSize { get; set; }
        public FontWeight FontWeight { get; set; }
    }
}