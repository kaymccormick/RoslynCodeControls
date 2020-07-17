using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace RoslynCodeControls
{
    public class RoslynPaginator : DocumentPaginator
    {
        private readonly ICodeView _control;
        public RenderTargetBitmap _bmp;

        public RoslynPaginator(ICodeView control, Size? pageSize = null, Thickness? margins = null)
        {
            _hdpi = 96;
            _vdpi = 96;
            _defaultMargins = new Thickness(0.25 * _hdpi, 0.25 * _vdpi, 0.25 * _hdpi, 0.25 * _vdpi);
            var ps = pageSize.GetValueOrDefault(_defaultPageSize);
            var m = margins.GetValueOrDefault(_defaultMargins);
            _control = control;
            DocPageSize = new Size(ps.Width - m.Left - m.Right,
                ps.Height - m.Top - m.Bottom);
            Debug.WriteLine(
                $"PAge size is {DocPageSize} or {DocPageSize.Width / _hdpi}\"x{DocPageSize.Height / _vdpi}\"");
            if (_control.TextDestination.Bounds.IsEmpty) throw new InvalidOperationException();
            var intWidth
                = (int) _control.TextDestination.Bounds.Width;
            Debug.WriteLine("Width of textdest is " + intWidth);
            var intHeight
                = (int) _control.TextDestination.Bounds.Height;
            Debug.WriteLine("Height of textdest is " + intHeight);

            _margins = m;
            _pageSize = ps;
            var b = new DrawingBrush
            {
                Drawing = _control.TextDestination,
                Viewbox = _control.TextDestination.Bounds,
                ViewboxUnits = BrushMappingMode.Absolute,
                Viewport = _control.TextDestination.Bounds,
                ViewportUnits = BrushMappingMode.Absolute
            };


            var v = new DrawingVisual();
            var dpi = VisualTreeHelper.GetDpi(v);
            var bmp = new RenderTargetBitmap(intWidth
                , intHeight
                , dpi.PixelsPerInchX, dpi.PixelsPerInchY, PixelFormats.Pbgra32);

            var dc = v.RenderOpen();
            dc.DrawRectangle(b, null, _control.TextDestination.Bounds);
            var formattedText = new FormattedText($"{intWidth}x{intHeight} @ {dpi.PixelsPerInchX}x{dpi.PixelsPerInchY}",
                CultureInfo.CurrentCulture, FlowDirection.LeftToRight,
                new Typeface(new FontFamily("Arial"), FontStyles.Normal, FontWeights.Normal, FontStretches.Normal),
                16, Brushes.Yellow
                , null, TextFormattingMode.Ideal, 1);
            var origin = new Point(5, 5);
            var bg = new Rect(origin, new Size(formattedText.Width, formattedText.Height));
            dc.DrawRectangle(Brushes.Black, null, bg);
            dc.DrawText(
                formattedText, origin);
            ;
            dc.Close();

            bmp.Render(v);
            _bmp = bmp;
            var zz = new PngBitmapEncoder();
            zz.Frames.Add(BitmapFrame.Create(bmp));
            // FileStream stream = new FileStream(@"c:\temp\new.png", FileMode.Create);
            // zz.Save(stream);
            PageCount = (int) (b.Drawing.Bounds.Height / DocPageSize.Height + 1) + 1;
            Source = control as IDocumentPaginatorSource;
        }


        public override DocumentPage GetPage(int pageNumber)
        {
            return GetDocumentPage(pageNumber, out _);
        }

        public DocumentPage GetDocumentPage(int pageNumber, out Info1 info)
        {
            if (pageNumber == 0)
            {
                DrawingVisual dv1 = new DrawingVisual();
                var dc0 =dv1.RenderOpen();

                // dc0.PushTransform(new TranslateTransform());

                // var p = new FixedPage();
                var text = new FormattedText(_control.DocumentTitle, CultureInfo.CurrentCulture, FlowDirection.LeftToRight,new Typeface(new FontFamily("Times new Roman"), FontStyles.Normal, FontWeights.Normal, FontStretches.Normal), 32, Brushes.Black, null,TextFormattingMode.Ideal,1); ;
                dc0.DrawText(text,new Point((PageSize.Width - text.Width)/2,4*_vdpi));
                dc0.Close();
                info = null;
                // var v = new VisualTarget(new HostVisual());
                // v.RootVisual = d;
                var dp1 = new DocumentPage(dv1, PageSize, new Rect(0, -1 * _vdpi, PageSize.Width, PageSize.Height + _vdpi),
                    new Rect(0, 0, PageSize.Width, PageSize.Height));
                return dp1;
                
            }

            // if (pageNumber != 0)
            // throw new InvalidOperationException();
            // var w = _control.MaxX;
            // var h = _control.MaxY;
            // var p = h /
            // PageSize.Height;
            var b = new ImageBrush {ImageSource = _bmp};
            pageNumber--;

            var rectangle = new Rect(_margins.Left, _margins.Top,
                DocPageSize.Width, DocPageSize.Height);
            var viewPort = new Rect(0, pageNumber * DocPageSize.Height, _bmp.PixelWidth, _bmp.PixelHeight);
            var width = Math.Min(_bmp.PixelWidth, DocPageSize.Width);
            var viewBox = new Rect(-1 * _margins.Left,
                -1 * pageNumber * DocPageSize.Height - _margins.Top, width,
                DocPageSize.Height + _margins.Top);

            Debug.WriteLine("port" + viewPort);
            Debug.WriteLine("box" + viewBox);
            b.Viewport = viewPort;
            b.ViewportUnits = BrushMappingMode.Absolute;

            b.Viewbox = viewBox;
            Debug.WriteLine("Viewbox = " + viewBox);
            b.ViewboxUnits = BrushMappingMode.Absolute;
            // RenderTargetBitmap renderTarget = new RenderTargetBitmap((int)w, (int)h, 96, 96, PixelFormats.Pbgra32);

            var dv = new DrawingVisual();
            var dc = dv.RenderOpen();
            dc.DrawRectangle(Brushes.Yellow, new Pen(Brushes.Orange, 2),
                new Rect(0, -1 * _vdpi, PageSize.Width, 1 * _vdpi));

            Debug.WriteLine(rectangle);
            dc.DrawRectangle(b,
                null, rectangle);
            // dc.DrawRectangle(null, new Pen(Brushes.Black, 2), rectangle);
            var cKayMccormick = "Publishing and Layout \x00a9 2020 Kay McCormick";
            var formattedText = new FormattedText(cKayMccormick, CultureInfo.CurrentCulture, FlowDirection.LeftToRight,
                new Typeface(new FontFamily("Arial"), FontStyles.Normal, FontWeights.Normal, FontStretches.Normal), 12,
                Brushes.Black, new NumberSubstitution(), TextFormattingMode.Ideal, 1);
            var formattedText0 = new FormattedText(_control.DocumentTitle ?? "", CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                new Typeface(new FontFamily("Arial"), FontStyles.Normal, FontWeights.Normal, FontStretches.Normal), 16,
                Brushes.Black, new NumberSubstitution(), TextFormattingMode.Ideal, 1);
            dc.DrawText(formattedText0, new Point(0.15 * _hdpi, (_margins.Top - formattedText0.Height) / 2 * _hdpi));
            dc.DrawText(formattedText,
                new Point(0.15 * _hdpi, rectangle.Bottom + (_margins.Bottom - formattedText.Height) / 2));
            var formattedText2 = new FormattedText((pageNumber + 1).ToString(), CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                new Typeface(new FontFamily("Arial"), FontStyles.Normal, FontWeights.Normal, FontStretches.Normal), 12,
                Brushes.Black, new NumberSubstitution(), TextFormattingMode.Ideal, 1);
            dc.Close();
            // PngBitmapEncoder zz = new PngBitmapEncoder();
            // var b1 = new RenderTargetBitmap((int) PageSize.Width, (int) PageSize.Height, 96, 96, PixelFormats.Pbgra32);
            // b1.Render(dv);

            // zz.Frames.Add(BitmapFrame.Create(b1));
            // FileStream stream = new FileStream(@"c:\temp\page" + pageNumber + ".png", FileMode.Create);
            // zz.Save(stream);
            info = new Info1 {rectangle = rectangle, brush = b};
            var dp = new DocumentPage(dv, PageSize, new Rect(0, -1 * _vdpi, PageSize.Width, PageSize.Height + _vdpi),
                new Rect(0, 0, PageSize.Width, PageSize.Height));
            return dp;
        }

        public Size DocPageSize { get; }

        public override bool IsPageCountValid { get; } = true;

        public override int PageCount { get; }
        // {
        // get
        // { 
        // var w = PageSize.Width;
        // var h = _control.MaxY;
        // var p = h / PageSize.Height;

        // return (int) (p + 1);

        // }
        // }


        private readonly Size _defaultPageSize = new Size(8.5 * 96, 11 * 96);
        private readonly Thickness _defaultMargins;
        private Size _pageSize;
        private Thickness _margins;
        private static int _vdpi;
        private static int _hdpi;

        public override Size PageSize
        {
            get { return _pageSize; }
            set { _pageSize = value; }
        }

        public override IDocumentPaginatorSource Source { get; }
    }

    public class Info1
    {
        public ImageBrush brush { get; set; }
        public Rect rectangle { get; set; }
    }

    public class CoverPage : Control
    {
    }
}