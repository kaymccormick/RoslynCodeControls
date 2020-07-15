using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace RoslynCodeControls
{
    public class RoslynPaginator : DocumentPaginator
    {
        private readonly CodeIface _control;
        private RenderTargetBitmap _bmp;

        public RoslynPaginator(CodeIface control)
        {
            _control = control;
            DocPageSize = new Size(PageSize.Width - marginleft * dpi - marginRight * dpi,
                PageSize.Height - marginTop * dpi -marginBottom *dpi);
            Debug.WriteLine(DocPageSize);
            var intWidth
                = (int)_control.TextDestination.Bounds.Width;
            var intHeight
                = (int)_control.TextDestination.Bounds.Height;
            RenderTargetBitmap bmp = new RenderTargetBitmap(intWidth
, intHeight
, 96, 96, PixelFormats.Pbgra32);

            var b = new DrawingBrush();
            b.Drawing = _control.TextDestination;

            b.Viewbox = _control.TextDestination.Bounds;
            b.ViewboxUnits = BrushMappingMode.Absolute;
            b.Viewport = _control.TextDestination.Bounds;
            b.ViewportUnits = BrushMappingMode.Absolute;

            DrawingVisual v = new DrawingVisual();
            var dc = v.RenderOpen();
            dc.DrawRectangle(b, null, _control.TextDestination.Bounds);
            dc.Close();

            bmp.Render(v);
            _bmp = bmp;
            PngBitmapEncoder zz= new PngBitmapEncoder();
            zz.Frames.Add(BitmapFrame.Create(bmp));
            FileStream stream = new FileStream(@"c:\temp\new.png", FileMode.Create);
            zz.Save(stream);
            PageCount = (int) (b.Drawing.Bounds.Height / DocPageSize.Height +1);
            Source = control as IDocumentPaginatorSource;
        }

        private double marginTop = 1;
        private double marginBottom = 1;
        private double marginleft =1;
        private double marginRight = 1;
        public override DocumentPage GetPage(int pageNumber)
        {
            
            // if (pageNumber != 0)
            // throw new InvalidOperationException();
            // var w = _control.MaxX;
            // var h = _control.MaxY;
            // var p = h / PageSize.Height;
            var b = new ImageBrush();
            b.ImageSource = _bmp;


            //- marginTop * dpi - marginBottom * dpi) 
            var pageSizeHeight = DocPageSize.Height* pageNumber;
            
           
            // b.Viewbox = bViewbox;
            // b.ViewboxUnits=BrushMappingMode.Absolute;
            
            var rectangle = new Rect(marginTop * dpi, marginleft * dpi, DocPageSize.Width, DocPageSize.Height);
            var viewPort = new Rect(0,0, _bmp.PixelWidth, _bmp.PixelHeight);
            var width = Math.Min(_bmp.PixelWidth, DocPageSize.Width);
            var viewBox = new Rect(-1 * marginleft*dpi,  -1 * pageNumber * DocPageSize.Height - marginTop * dpi, width, DocPageSize.Height + marginTop*dpi);

            Debug.WriteLine("port" + viewPort);
            Debug.WriteLine("box" + viewBox);
            b.Viewport= viewPort;
            b.ViewportUnits=BrushMappingMode.Absolute;

            b.Viewbox = viewBox;
            Debug.WriteLine("Viewbox = " + viewBox);
            b.ViewboxUnits=BrushMappingMode.Absolute;
            // RenderTargetBitmap renderTarget = new RenderTargetBitmap((int)w, (int)h, 96, 96, PixelFormats.Pbgra32);

            var dv = new DrawingVisual();
            var dc = dv.RenderOpen();
            Debug.WriteLine(rectangle);
            dc.DrawRectangle(b, 
                null, rectangle);
            dc.DrawRectangle(null, new Pen(Brushes.Black, 2), rectangle);
            var cKayMccormick = "(c) Kay McCormick";
            var formattedText = new FormattedText(cKayMccormick,CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface(new FontFamily("Arial"), FontStyles.Normal, FontWeights.Normal, FontStretches.Normal),12, Brushes.Black, new NumberSubstitution(),TextFormattingMode.Ideal,1 );
            dc.DrawText(formattedText,
                 new Point(10, rectangle.Bottom + 50));
            var formattedText2 = new FormattedText((pageNumber+1).ToString(), CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface(new FontFamily("Arial"), FontStyles.Normal, FontWeights.Normal, FontStretches.Normal), 12, Brushes.Black, new NumberSubstitution(), TextFormattingMode.Ideal, 1);
            dc.Close();
            PngBitmapEncoder zz = new PngBitmapEncoder();
            var b1 = new RenderTargetBitmap((int) PageSize.Width, (int) PageSize.Height, 96, 96, PixelFormats.Pbgra32);
            b1.Render(dv);

            zz.Frames.Add(BitmapFrame.Create(b1));
            FileStream stream = new FileStream(@"c:\temp\page" + pageNumber + ".png", FileMode.Create);
            zz.Save(stream);
            var dp = new DocumentPage(dv, PageSize, Rect.Empty, new Rect(0, 0, PageSize.Width/2, PageSize.Height/2));
            return dp;

        }

        public Size DocPageSize { get;  }

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

        private int dpi = 96;
        public override Size PageSize { get; set; } = new Size(8.5 * 96, 11 * 96);
        public override IDocumentPaginatorSource Source { get; }
    }
}