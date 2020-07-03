using System.Windows;
using System.Windows.Media;

namespace RoslynCodeControls
{
    /// <summary>
    /// 
    /// </summary>
    public class TextCaret : UIElement
    {
        private int _caretWidth = 3;
        private Pen _pen;

        public TextCaret(double lineHeight)
        {
            this.lineHeight = lineHeight;
            _pen = new Pen(Brushes.Black,  3);
        }

        public TextCaret()
        {
            _pen = new Pen(Brushes.Black,  lineHeight);
        }

        protected override Size MeasureCore(Size availableSize)
        {
            return new Size(_caretWidth + 1, lineHeight);
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);
            var c = VisualTreeHelper.GetContentBounds(this);
            drawingContext.DrawLine(_pen, new Point(0, 0), new Point(0, lineHeight));

        }

        public double lineHeight { get; set; }
    }
}