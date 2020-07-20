using System;
using System.Threading.Channels;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media.TextFormatting;

namespace RoslynCodeControls
{
    public interface IFontDetails
    {
        double PixelsPerDip { get; }
        double FontSize { get; }
        string FaceName { get; }
        FontWeight FontWeight { get; }
    }

    public class MainUpdateParameters : IMainUpdateParameters, IFontDetails
    {
        public MainUpdateParameters(int textStorePosition, int lineNo,
            Point linePosition, TextFormatter textFormatter, double paragraphWidth, double pixelsPerDip, double emSize0,
            string faceName, ChannelWriter<UpdateInfo> channelWriter, FontWeight fontWeight,
            DocumentPaginator paginator, TextSourceInitializationParameters textSourceInitializationParameters,
            Action<string> debugFn=null,
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

        public int TextStorePosition { get; set; }
        public int LineNo { get; set; }
        public Point LinePosition { get; set; }
        public TextFormatter TextFormatter { get; private set; }
        public double ParagraphWidth { get; private set; }
        public double PixelsPerDip { get; private set; }
        public double FontSize { get; private set; }
        public string FaceName { get; private set; }
        public ChannelWriter<UpdateInfo> ChannelWriter { get; private set; }
        public FontWeight FontWeight { get; private set; }
        public DocumentPaginator Paginator { get; private set; }
        public TextSourceInitializationParameters TextSourceInitializationParameters { get; private set; }
        public Action<string> DebugFn { get; }
        public Size? PageSize { get; private set; }
        public bool Paginate { get; private set; }
    }
}