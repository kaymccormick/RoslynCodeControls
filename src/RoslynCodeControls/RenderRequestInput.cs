using System;
using System.Windows;
using System.Windows.Media.TextFormatting;

namespace RoslynCodeControls
{
    public readonly struct RenderRequestInput 
        // : ITimestampedRequest
    {
        public void Deconstruct(out RoslynCodeControl roslynCodeControl, out int lineNo, out int offset, out double y, out double x, out TextFormatter textFormatter, out double paragraphWidth, out double pixelsPerDip, out CustomTextSource4 customTextSource4, out double maxY, out double maxX, out string fontFamilyName, out double fontSize, out FontWeight fontWeight)
        {
            roslynCodeControl = RoslynCodeControl;
            lineNo = LineNo;
            offset = Offset;
            y = Y;
            x = X;
            textFormatter = TextFormatter;
            paragraphWidth = ParagraphWidth;
            pixelsPerDip = PixelsPerDip;
            customTextSource4 = CustomTextSource4;
            maxY = MaxY;
            maxX = MaxX;
            fontFamilyName = FontFamilyName;
            fontSize = FontSize;
            fontWeight = FontWeight;
        }

        public RenderRequestInput(RoslynCodeControl roslynCodeControl, int lineNo, int offset, double y, double x, TextFormatter textFormatter, double paragraphWidth,
            double pixelsPerDip, CustomTextSource4 customTextSource4, double maxY, double maxX, double fontSize,
            string fontFamilyName, FontWeight fontWeight)
        {
            // Timestamp = DateTime.Now;
            RoslynCodeControl = roslynCodeControl;
            LineNo = lineNo;
            Offset = offset;
            Y = y;
            X = x;
            TextFormatter = textFormatter;
            ParagraphWidth = paragraphWidth;
            PixelsPerDip = pixelsPerDip;
            CustomTextSource4 = customTextSource4;
            MaxY = maxY;
            MaxX = maxX;
            FontSize = fontSize;
            FontFamilyName = fontFamilyName;
            FontWeight = fontWeight;
        }

        // public DateTime Timestamp { get; }


        public RoslynCodeControl RoslynCodeControl { get;}
        public int LineNo { get;  }
        public int Offset { get;  }
        public double Y { get;  }
        public double X { get;  }
        public TextFormatter TextFormatter { get;  }
        public double ParagraphWidth { get;  }
        public double PixelsPerDip { get;  }
        public CustomTextSource4 CustomTextSource4 { get;  }
        public double MaxY { get;  }
        public double MaxX { get;  }
        public string FontFamilyName { get;  }
        public double FontSize { get;  }
        public FontWeight FontWeight { get; }
    }

    public interface ITimestampedRequest
    {
        DateTime Timestamp { get; }
    }

    public class RequestBase
    {
    }
}