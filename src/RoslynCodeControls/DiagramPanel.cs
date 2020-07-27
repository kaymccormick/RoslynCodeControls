using System.Windows;
using System.Windows.Controls;

namespace RoslynCodeControls
{
    public class DiagramPanel : Panel
    {
        /// <inheritdoc />
        protected override Size ArrangeOverride(Size finalSize)
        {
            double y = 0;
            double x = 0;
            foreach (UIElement child in Children)
            {
                if (child != null)
                {
                    child.Measure(finalSize);
                    var desiredSize = child.DesiredSize;
                    if (desiredSize.Width + x >= finalSize.Width)
                    {

                    }
                    else
                    {

                    }
                }
            }

            return base.ArrangeOverride(finalSize);
        }

        /// <inheritdoc />
        protected override Size MeasureOverride(Size availableSize)
        {
            return base.MeasureOverride(availableSize);
        }
    }
}