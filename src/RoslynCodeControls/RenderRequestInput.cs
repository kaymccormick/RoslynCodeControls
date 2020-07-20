using System.Windows;
using System.Windows.Media.TextFormatting;

namespace RoslynCodeControls
{
    public readonly struct RenderRequestInput
    {
        public RenderRequestInput(RoslynCodeControl roslynCodeControl, int lineNo, int offset, double y, double x,
            LineInfo2 lineInfo, TextFormatter textFormatter, double paragraphWidth,
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
            PixelsPerDip = pixelsPerDip;
            CustomTextSource4 = customTextSource4;
            MaxY = maxY;
            MaxX = maxX;
            FontSize = fontSize;
            FontFamilyName = fontFamilyName;
            FontWeight = fontWeight;
        }


        public RoslynCodeControl RoslynCodeControl { get;}
        public int LineNo { get;  }
        public int Offset { get;  }
        public double Y { get;  }
        public double X { get;  }
        public LineInfo2 LineInfo { get;  }
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
}