using System;
using System.Threading.Channels;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media.TextFormatting;

namespace RoslynCodeControls
{
    public class MainUpdateParameters : IMainUpdateParameters, IFontDetails, IDebugParam
    {
        public MainUpdateParameters(int textStorePosition, int lineNo,
            Point linePosition, TextFormatter textFormatter, double paragraphWidth, double pixelsPerDip, double emSize0,
            string faceName, ChannelWriter<UpdateInfo> channelWriter, FontWeight fontWeight,
            DocumentPaginator paginator, TextSourceInitializationParameters textSourceInitializationParameters,
            RoslynCodeBase.DebugDelegate debugFn=null,
            Size? pageSize = null, bool paginate = false)
        {
            TextStorePosition = textStorePosition;
            LineNo = lineNo;
            LinePosition = linePosition;
            TextFormatter = textFormatter;
            ParagraphWidth = paragraphWidth;
            PixelsPerDip = pixelsPerDip;
            FontSize = emSize0;
            FaceName = faceName;
            ChannelWriter = channelWriter;
            FontWeight = fontWeight;
            Paginator = paginator;
            TextSourceInitializationParameters = textSourceInitializationParameters;
            DebugFn = debugFn;
            PageSize = pageSize;
            Paginate = paginate;
        }

        public int TextStorePosition { get; }
        public int LineNo { get;  }
        public Point LinePosition { get;  }
        public TextFormatter TextFormatter { get;  }
        public double ParagraphWidth { get;  }
        public double PixelsPerDip { get;  }
        public double FontSize { get;  }
        public string FaceName { get;  }
        public ChannelWriter<UpdateInfo> ChannelWriter { get;  }
        public FontWeight FontWeight { get;  }
        public DocumentPaginator Paginator { get;  }
        public TextSourceInitializationParameters TextSourceInitializationParameters { get; private set; }
        public RoslynCodeBase.DebugDelegate DebugFn { get; }
        public Size? PageSize { get;  }
        public bool Paginate { get; }
    }

    public interface IDebugParam
    {
        RoslynCodeBase.DebugDelegate DebugFn { get; }
    }
}