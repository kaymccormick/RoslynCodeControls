using System;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Brushes = System.Windows.Media.Brushes;
using FontFamily = System.Windows.Media.FontFamily;
using Point = System.Windows.Point;
using Size = System.Windows.Size;

namespace RoslynCodeControls
{
    public class RoslynPaginatorExt : DocumentPaginator
    {
        private DocumentPaginator _documentPaginatorImplementation;

        public RoslynPaginatorExt(DocumentPaginator documentPaginatorImplementation)
        {
            _documentPaginatorImplementation = documentPaginatorImplementation;
        }

        /// <inheritdoc />
        public override DocumentPage GetPage(int pageNumber)
        {
            return _documentPaginatorImplementation.GetPage(pageNumber);
        }

        /// <inheritdoc />
        public override bool IsPageCountValid
        {
            get { return _documentPaginatorImplementation.IsPageCountValid; }
        }

        /// <inheritdoc />
        public override int PageCount
        {
            get { return _documentPaginatorImplementation.PageCount; }
        }

        /// <inheritdoc />
        public override Size PageSize
        {
            get { return _documentPaginatorImplementation.PageSize; }
            set { _documentPaginatorImplementation.PageSize = value; }
        }

        /// <inheritdoc />
        public override IDocumentPaginatorSource Source
        {
            get { return _documentPaginatorImplementation.Source; }
        }
    }
    public class RoslynPaginator : DocumentPaginator
    {
        private readonly ICodeView _control;
        public RenderTargetBitmap _bmp;
        private bool coverPage = false;

        public RoslynPaginator(ICodeView control, Size? pageSize = null, Thickness? margins = null)
        {
            _hdpi = 96;
            _vdpi = 96;

            var defaultSizeInches = new Size(8.5, 11);
            var defaultPageSize = new Size(defaultSizeInches.Width * _hdpi, defaultSizeInches.Height * _vdpi);
            var marginInches = 1.0;
            var defaultMargins = new Thickness(marginInches * _hdpi, marginInches * _vdpi, marginInches * _hdpi, marginInches * _vdpi);

            var ps = pageSize.GetValueOrDefault(defaultPageSize);
            var m = margins.GetValueOrDefault(defaultMargins);
            
            _control = control;
            DocPageSize = new Size(ps.Width - m.Left - m.Right,
                ps.Height - m.Top - m.Bottom);
            Debug.WriteLine(
                $"PAge size is {DocPageSize} or {DocPageSize.Width / _hdpi}\"x{DocPageSize.Height / _vdpi}\"");
            if (_control.TextDestination.Bounds.IsEmpty) throw new InvalidOperationException();
            var visual = (Control)_control;
            var sourceDpi = VisualTreeHelper.GetDpi(visual);
            double w = _control.TextDestination.Bounds.Width;
            double h0 = _control.TextDestination.Bounds.Height;
            int pixelWidth;
            int pixelHeight;
            using (var graphics = Graphics.FromHwnd(IntPtr.Zero))
            {
                pixelWidth = (int)(w * graphics.DpiX / 96.0);
                pixelHeight = (int)(h0 * graphics.DpiY / 96.0);
            }

            var intWidth = (int) _control.TextDestination.Bounds.Width;
            Debug.WriteLine("Width of textdest is " + intWidth);
            var intHeight0
                = (int) _control.TextDestination.Bounds.Height;
            Debug.WriteLine("Height of textdest is " + intHeight0);

            _margins = m;
            _pageSize = ps;
            var b = new DrawingBrush
            {
                Drawing = _control.TextDestination,
                // Viewbox = _control.TextDestination.Bounds,
                // ViewboxUnits = BrushMappingMode.Absolute
            };

            bool fRando = false;
            if (fRando)
            {
                var v = new DrawingVisual();
                var dpi = VisualTreeHelper.GetDpi(v);
                var dpiPixelsPerInchX = dpi.PixelsPerInchX;
                var dpiPixelsPerInchY = dpi.PixelsPerInchY;

                var p1 = Math.Floor(intHeight0 / DocPageSize.Height);
                if (Math.Abs(intHeight0 % DocPageSize.Height) > 0.5) p1++;

                var h = (int) Math.Floor(p1 * DocPageSize.Height);

                var dc = v.RenderOpen();
                var rectangle = new Rect(0, 0, _control.TextDestination.Bounds.Width,
                    _control.TextDestination.Bounds.Height);
                dc.DrawRectangle(b, null, rectangle);
                var formattedText = new FormattedText($"{intWidth}x{h} @ {dpiPixelsPerInchX}x{dpiPixelsPerInchY}",
                    CultureInfo.CurrentCulture, FlowDirection.LeftToRight,
                    new Typeface(new FontFamily("Arial"), FontStyles.Normal, FontWeights.Normal, FontStretches.Normal),
                    16, Brushes.Yellow
                    , null, TextFormattingMode.Ideal, 1);
                var origin = new Point(5, 5);
                var bg = new Rect(origin, new Size(formattedText.Width, formattedText.Height));
                dc.DrawRectangle(Brushes.Black, null, bg);
                dc.DrawText(
                    formattedText, origin);
                dc.Close();


                var bmp = new RenderTargetBitmap(pixelWidth
                    , pixelHeight
                    , dpiPixelsPerInchX * 2, dpiPixelsPerInchY * 2, PixelFormats.Pbgra32);
                bmp.Render(v);
                _bmp = bmp;

               
            } else
            {

                DrawingImage di = new DrawingImage(_control.TextDestination);
                DrawingVisual v = new DrawingVisual();
                var dc = v.RenderOpen();
                dc.DrawImage(di, _control.TextDestination.Bounds
                );
                dc.Close();

                var bmp = new RenderTargetBitmap(pixelWidth
                    , pixelHeight
                    , _hdpi, _vdpi, PixelFormats.Pbgra32);
                bmp.Render(v);
                _bmp = bmp;
            }

            var zz = new TiffBitmapEncoder();
            var bitmapFrame = BitmapFrame.Create(_bmp);
            zz.Frames.Add(bitmapFrame);
            using (var stream = new FileStream(@"c:\temp\new.tiff", FileMode.Create))
            {
                zz.Save(stream);
            }
            PageCount = (int) (b.Drawing.Bounds.Height / DocPageSize.Height + 1) + (coverPage ? 1 : 0);
            Source = control as IDocumentPaginatorSource;
        }


        public override DocumentPage GetPage(int pageNumber)
        {
            Debug.WriteLine("page " + pageNumber);
            return GetDocumentPage(pageNumber, out _);
        }

        public DocumentPage GetDocumentPage(int pageNumber, out Info1 info)
        {
            if (pageNumber == 0 && coverPage)
            {
                return CoverPageExt.CreateCoverPage(_hdpi, _margins, _vdpi, PageSize, _control, out info);
            }

            var dv = new DrawingVisual();
            var dc = dv.RenderOpen();
            
            // DrawingGroup dg = new DrawingGroup();
            // var d = VisualTreeHelper.GetDpi((Visual)_control);
            // var dpiScale = new DpiScale(d.DpiScaleX * 2, d.DpiScaleY * 2);
            // VisualTreeHelper.SetRootDpi(dv, dpiScale);

            DrawPage(pageNumber, dc, out info);

            dc.Close();

            var bleedBox = new Rect(0, 0, PageSize.Width, PageSize.Height);
            var contentBox = new Rect(0, 0, PageSize.Width, PageSize.Height);

            // var contentBox =  new Rect(_margins.Left, _margins.Top,
                // DocPageSize.Width, DocPageSize.Height);

            return new DocumentPage(dv, PageSize, bleedBox, contentBox);
        }

        private void DrawPage(int pageNumber, DrawingContext dc, out Info1 info)
        {
            double pixelsPerDip = 1.0;
            if (coverPage)
                pageNumber--;

            var topOfPage = pageNumber * DocPageSize.Height;
            var width = Math.Min(_bmp.Width, DocPageSize.Width);
            var viewBox = new Rect(0, topOfPage, width, DocPageSize.Height);

            var b = new ImageBrush
            {
                ImageSource = _bmp,
                AlignmentY = AlignmentY.Top,
                AlignmentX = AlignmentX.Left,
                Stretch = Stretch.Uniform,
                Viewbox = viewBox,
                ViewboxUnits = BrushMappingMode.Absolute
            };

            var rectangle = new Rect(_margins.Left, _margins.Top,
                DocPageSize.Width, DocPageSize.Height);

            Debug.WriteLine("box = " + viewBox);
            Debug.WriteLine(rectangle);
            var fTransform = true;
            // ReSharper disable once ConditionIsAlwaysTrueOrFalse
            if (fTransform)
            {
                dc.PushTransform(new TranslateTransform(_margins.Left, _margins.Top));
            }

            var rect = rectangle;
            rect.Offset(-1 * _margins.Left, -1 * _margins.Top);
            rect.Width = width;
            dc.DrawRectangle(b, null, rect);

            // ReSharper disable once ConditionIsAlwaysTrueOrFalse
            if (fTransform)
            {
                dc.Pop();
            }

            DrawDocumentTitle(dc, pixelsPerDip);
            DrawFooter(dc, rectangle, pixelsPerDip);
            DrawPageNumber(pageNumber);

            info = new Info1 {rectangle = rectangle, brush = b};
        }

        private static void DrawPageNumber(int pageNumber)
        {
            var formattedText2 = new FormattedText((pageNumber + 1).ToString(), CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                new Typeface(new FontFamily("Arial"), FontStyles.Normal, FontWeights.Normal, FontStretches.Normal), 12,
                Brushes.Black, new NumberSubstitution(), TextFormattingMode.Ideal, 1);
        }

        private void DrawFooter(DrawingContext dc, Rect rectangle, double pixelsPerDip)
        {
            var cKayMccormick = "Publishing and Layout \x00a9 2020 Kay McCormick";
            var typeface = new Typeface(new FontFamily("Arial"),
                FontStyles.Normal, 
                FontWeights.Normal, FontStretches.Normal);

            var formattedText = new FormattedText(
                cKayMccormick, 
                CultureInfo.CurrentCulture, 
                FlowDirection.LeftToRight,
                typeface, 12,
                Brushes.Black, null, 
                TextFormattingMode.Ideal, 
                pixelsPerDip);
            
            var origin = new Point(0.15 * _hdpi, 
                rectangle.Bottom + (_margins.Bottom - formattedText.Height) / 2);
            dc.DrawText(formattedText, origin);
        }

        private void DrawDocumentTitle(DrawingContext dc, double pixelsPerDip)
        {
            var formattedText0 = new FormattedText(_control.DocumentTitle ?? "No title", CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                new Typeface(new FontFamily("Arial"), FontStyles.Normal, FontWeights.Normal, FontStretches.Normal), 16,
                Brushes.Black, new NumberSubstitution(), TextFormattingMode.Ideal, pixelsPerDip);
            var origin = new Point(0.15 * _hdpi,
                (_margins.Top - formattedText0.Height) / 2);
            dc.DrawText(formattedText0, 
                origin);
        }

        private Size DocPageSize { get; }

        public override bool IsPageCountValid { get; } = true;

        public override int PageCount { get; }

        private Size _pageSize;
        private Thickness _margins;
        private readonly int _vdpi;
        private readonly int _hdpi;

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

    public static class CoverPageExt
    {
        public static DocumentPage CreateCoverPage(int hdpi, Thickness thickness, int vdpi, Size pageSize, ICodeView codeView, out Info1 info)
        {
            var dv1 = new DrawingVisual();
            var dc0 = dv1.RenderOpen();

            var text = new FormattedText(codeView.DocumentTitle ?? "Untitled", CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                new Typeface(new FontFamily("Times new Roman"), FontStyles.Normal, FontWeights.Normal,
                    FontStretches.Normal), 32, Brushes.Black, null, TextFormattingMode.Ideal, 1);
            ;
            dc0.DrawText(text, new Point((pageSize.Width - text.Width) / 2, 4 * vdpi));

            var info1 =
                $"Margins:\t\tLeft: {thickness.Left / hdpi:N1};\t\tRight: {thickness.Right / hdpi:N1}; \r\n\t\tTop: {thickness.Top / vdpi:N1};\t\tBottom: {thickness.Bottom / vdpi:N1}\r\nPage Size:\t{pageSize.Width / hdpi:N1}\"x{pageSize.Height / vdpi:N1}\"\r\n";
            var text1 = new FormattedText(info1, CultureInfo.CurrentCulture, FlowDirection.LeftToRight,
                new Typeface(new FontFamily("Arial"), FontStyles.Normal, FontWeights.Normal, FontStretches.Normal),
                16, Brushes.Black, null, TextFormattingMode.Ideal, 1);
            ;
            dc0.DrawText(text1, new Point((pageSize.Width - text1.Width) / 2, 5 * vdpi));

            dc0.Close();
            info = null;


            return new DocumentPage(dv1, pageSize,
                new Rect(0, -1 * vdpi, pageSize.Width, pageSize.Height + vdpi),
                new Rect(0, 0, pageSize.Width, pageSize.Height));
        }
    }

 
}