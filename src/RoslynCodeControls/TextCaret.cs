using System.Windows;
using System.Windows.Media;

namespace RoslynCodeControls
{
    /// <summary>
    /// 
    /// </summary>
    public sealed class TextCaret : UIElement
    {
        private int _caretWidth = 3;
        private Pen _pen;

        public TextCaret(double lineHeight)
        {
            this.LineHeight = lineHeight;
            _pen = new Pen(Brushes.Black,  _caretWidth);
        }

        public TextCaret()
        {
            _pen = new Pen(Brushes.Black,  _caretWidth);
        }

        protected override Size MeasureCore(Size availableSize)
        {
            return new Size(_caretWidth, LineHeight);
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);
            var c = VisualTreeHelper.GetContentBounds(this);
            drawingContext.DrawLine(_pen, new Point(0, 0), new Point(0, LineHeight));

        }

        public static readonly DependencyProperty LineHeightProperty = DependencyProperty.Register(
            "LineHeight", typeof(double), typeof(TextCaret), new FrameworkPropertyMetadata(default(double), FrameworkPropertyMetadataOptions.AffectsMeasure|FrameworkPropertyMetadataOptions.AffectsRender,OnLineHeightChanged));

        public double LineHeight
        {
            get { return (double) GetValue(LineHeightProperty); }
            set { SetValue(LineHeightProperty, value); }
        }

        private static void OnLineHeightChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((TextCaret) d).OnLineHeightChanged((double) e.OldValue, (double) e.NewValue);
        }


        private void OnLineHeightChanged(double oldValue, double newValue)
        {
        }

    }
}