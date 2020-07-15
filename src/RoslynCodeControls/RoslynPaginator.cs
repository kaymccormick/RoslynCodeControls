using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

namespace RoslynCodeControls
{
    public class RoslynPaginator : DocumentPaginator
    {
        private readonly RoslynCodeControl _control;

        public RoslynPaginator(RoslynCodeControl control)
        {
            _control = control;
            Source = control as IDocumentPaginatorSource;
        }

        public override DocumentPage GetPage(int pageNumber)
        {
            // if (pageNumber != 0)
            // throw new InvalidOperationException();
            var w = _control.MaxX;
            var h = _control.MaxY;
            var p = h / PageSize.Height;
            var b = _control.MyDrawingBrush;
            b.Drawing = _control.TextDestination;
            
            b.Viewbox = new Rect(new Point(0,PageSize.Height * pageNumber),PageSize);
            b.ViewboxUnits=BrushMappingMode.Absolute;
            b.Viewport= new Rect(PageSize);
            b.ViewportUnits=BrushMappingMode.Absolute;

            // RenderTargetBitmap renderTarget = new RenderTargetBitmap((int)w, (int)h, 96, 96, PixelFormats.Pbgra32);

            var dv = new DrawingVisual();
            var dc = dv.RenderOpen();
            dc.DrawRectangle(b, 
                null, new Rect(PageSize));
            dc.Close();

            var dp = new DocumentPage(dv);
            return dp;

        }

        public override bool IsPageCountValid { get; } = true;
        public override int PageCount
        {
            get
            { var w = _control.Rectangle.Width = PageSize.Width;
                var h = _control.Rectangle.Height = _control.MaxY;
                var p = h / PageSize.Height;
                var b = _control.MyDrawingBrush;
                return (int) (p + 1);

            }
        }

        public override Size PageSize { get; set; } = new Size(8.5 * 96, 11 * 96);
        public override IDocumentPaginatorSource Source { get; }
    }
}