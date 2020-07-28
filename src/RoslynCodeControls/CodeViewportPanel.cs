using System;
using System.Linq;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace RoslynCodeControls
{
    public class CodeViewportPanel : Panel, IScrollInfo
    {
        private IScrollInfo? _scrollInfoImplementation = null;
        private RoslynCodeBase _codeControl;

        private ScrollData _scrollData = new ScrollData();

        /// <inheritdoc />
        protected override Size MeasureOverride(Size availableSize)
        {
            var haveControl = CodeControl != null;
            if (haveControl)
            {
            }

            var childrenCount = VisualTreeHelper.GetChildrenCount(this);
            if (childrenCount != 0)
                for (var i = 0; i < childrenCount; i++)
                {
                    var child = (UIElement) VisualTreeHelper.GetChild(this, i);
                    child.Measure(availableSize);
                    Debug.WriteLine(
                        $"{nameof(CodeViewportPanel)}-{haveControl} Child {i} desired size is {child.DesiredSize}");
                }

            return availableSize;
        }

        /// <inheritdoc />
        protected override Size ArrangeOverride(Size finalSize)
        {
            var s = base.ArrangeOverride(finalSize);
            var haveControl = CodeControl != null;
            var physicalOffset = 0.0;
            if (haveControl)
            {
                var stroke = CodeControl.Rectangle.StrokeThickness;
                var strokeLines = Math.Ceiling(stroke / CodeControl.LineHeight);
                var lineNo = _scrollData._offset.Y - strokeLines;
                if (lineNo >= 0)
                {
                    var lineNode = CodeControl.FindLine((int) lineNo);
                    var line = lineNode?.Value;
                    if (line != null)
                    {
                        physicalOffset = line.Origin.Y + stroke;
                        Debug.WriteLine($"{nameof(CodeViewportPanel)} physical offset is {physicalOffset}");
                    }
                }
                else
                {
                    var a = strokeLines * CodeControl.LineHeight;
                    var b = a - stroke;
                    var remainder = stroke % CodeControl.LineHeight;
                    var c =  lineNo * CodeControl.LineHeight;
                    var d = a + c;
                    physicalOffset = d;// c - stroke - (CodeControl.LineHeight - remainder)

                }
            }

            var size = finalSize;
            size.Height += physicalOffset;
            var childrenCount = VisualTreeHelper.GetChildrenCount(this);
            if (childrenCount != 0)
                for (var i = 0; i < childrenCount; i++)
                {
                    var child = (UIElement) VisualTreeHelper.GetChild(this, i);
                    child.Arrange(new Rect(new Point(0, -1 * physicalOffset), size));
                }

            ScrollOwner?.InvalidateScrollInfo();
            
            return finalSize;
        }


        internal RoslynCodeBase CodeControl
        {
            get { return _codeControl; }
            set
            {
                _codeControl = value;
                ScrollOwner?.InvalidateScrollInfo();
            }
        }

        /// <inheritdoc />
        public void LineDown()  
        {
            var stroke = CodeControl.Rectangle.StrokeThickness;

            double viewPortHeight = ViewportHeight;
            
                // var a = CodeControl.LineInfos2?.Last?.Value?.Height;
                // var b = a.HasValue ? a.Value - CodeControl.LineHeight : 0;
                // var c = ActualHeight;
                // viewPortHeight = Math.Floor(c / CodeControl.LineHeight);
            
            double extentHeight;
            var e = CodeControl.DrawingBrushViewbox.Top + stroke;
                extentHeight = CodeControl?.LineInfos2?.Count() ?? 0.0;
                var d = CodeControl.Rectangle.StrokeThickness * 2 / CodeControl.LineHeight;
                extentHeight += Math.Ceiling(d);
                Debug.WriteLine($"Extentheight is {extentHeight}"); ;
            
            var offsetY = _scrollData._offset.Y + viewPortHeight;
            Debug.WriteLine($"LineDown {offsetY} < {extentHeight}");
            if(offsetY < extentHeight)
                SetVerticalOffset(_scrollData._offset.Y + 1);
        }

        /// <inheritdoc />
        public void LineLeft()
        {
        }

        /// <inheritdoc />
        public void LineRight()
        {
        }

        /// <inheritdoc />
        public void LineUp()
        {
            if(_scrollData._offset.Y > 0)
            SetVerticalOffset(_scrollData._offset.Y - 1);
        }

        /// <inheritdoc />
        public Rect MakeVisible(Visual visual, Rect rectangle)
        {
            return default;
        }

        /// <inheritdoc />
        public void MouseWheelDown()
        {
            SetVerticalOffset(_scrollData._offset.Y + 3);
        }

        /// <inheritdoc />
        public void MouseWheelLeft()
        {
        }

        /// <inheritdoc />
        public void MouseWheelRight()
        {
        }

        /// <inheritdoc />
        public void MouseWheelUp()
        {
            SetVerticalOffset(_scrollData._offset.Y - 3);
        }

        /// <inheritdoc />
        public void PageDown()
        {
            SetVerticalOffset(_scrollData._offset.Y + ViewportHeight);
        }

        /// <inheritdoc />
        public void PageLeft()
        {
        }

        /// <inheritdoc />
        public void PageRight()
        {
        }

        /// <inheritdoc />
        public void PageUp()
        {
            SetVerticalOffset(_scrollData._offset.Y - ViewportHeight);
        }

        /// <inheritdoc />
        public void SetHorizontalOffset(double offset)
        {
        }

        /// <inheritdoc />
        public void SetVerticalOffset(double offset)
        {
            _scrollData._offset.Y = Math.Round(Math.Min(Math.Max(0, ExtentHeight-ViewportHeight), Math.Max(offset,0)));
            InvalidateMeasure();
            ScrollOwner.InvalidateScrollInfo();

            Debug.WriteLine($"{nameof(SetVerticalOffset)} ( {offset} )");
            return;
            var childrenCount = VisualTreeHelper.GetChildrenCount(this);
            if (childrenCount != 0)
                for (var i = 0; i < childrenCount; i++)
                {
                    var child = (FrameworkElement) VisualTreeHelper.GetChild(this, i);
                    var bounds = VisualTreeHelper.GetContentBounds(child);
                    bounds.Y = offset;
                    child.Arrange(bounds);
                }
        }

        /// <inheritdoc />
        public bool CanHorizontallyScroll { get; set; }

        /// <inheritdoc />
        public bool CanVerticallyScroll{ get; set; }
        // {
            // get { return ViewportHeight + VerticalOffset < ExtentHeight; }
            // set {  }
        // }

        /// <inheritdoc />
        public double ExtentHeight
        {
            get
            {
                var stroke = CodeControl.Rectangle.StrokeThickness;
                double extentHeight;
                var e = CodeControl.DrawingBrushViewbox.Top + stroke;
                extentHeight = CodeControl?.LineInfos2?.Count() ?? 0.0;
                
                var d = stroke * 2 / CodeControl.LineHeight;
                return extentHeight + Math.Ceiling(d);

                var actualHeight = VisualTreeHelper.GetChildrenCount(this) > 0
                    ? ((FrameworkElement) VisualTreeHelper.GetChild(this, 0)).ActualHeight
                    : 0;
                Debug.WriteLine($"ExtentHeight = {actualHeight}");
                return actualHeight;
            }
        }

        /// <inheritdoc />
        public double ExtentWidth
        {
            get
            {
                var actualWidth = VisualTreeHelper.GetChildrenCount(this) > 0
                    ? ((FrameworkElement)VisualTreeHelper.GetChild(this, 0)).ActualWidth
                    : 0;
                return actualWidth;
            }
        }

        /// <inheritdoc />
        public double HorizontalOffset
        {
            get { return _scrollData._offset.X; }
        }

        /// <inheritdoc />
        public ScrollViewer ScrollOwner { get; set; } = null!;

        /// <inheritdoc />
        public double VerticalOffset
        {
            get { return _scrollData._offset.Y; }
        }

        /// <inheritdoc />
        public double ViewportHeight
        {
            get
            {
                if (CodeControl == null)
                    return 0;

                var a = CodeControl.LineInfos2?.Last?.Value?.Height;
                var b = a.HasValue ? a.Value - CodeControl.LineHeight : 0;
                var c = ActualHeight;
                return Math.Floor(c / CodeControl.LineHeight);


                // var controlLineSpacing = CodeControl.LineInfos2?.Last?.Value?.Height;
                // var codeControlLineSpacing = controlLineSpacing.HasValue ? controlLineSpacing.Value  - CodeControl.LineSpacing : 0;
                // var actualHeight = ActualHeight + CodeControl.DrawingBrushViewbox.Top - codeControlLineSpacing;
                // var viewPortHeight = actualHeight / CodeControl.LineHeight;
                // Debug.WriteLine($"{actualHeight} / {CodeControl.LineHeight} Viewport height is {viewPortHeight}");
                // return viewPortHeight;
            }
        }

        /// <inheritdoc />
        public double ViewportWidth
        {
            get { return ActualWidth; }
        }

        private class ScrollData
        {
            internal Vector _offset = new Vector(0, 0);
        }

#if false
        /// <inheritdoc />
        public void LineDown()
        {
            _scrollInfoImplementation?.LineDown();
        }

        /// <inheritdoc />
        public void LineLeft()
        {
            _scrollInfoImplementation?.LineLeft();
        }

        /// <inheritdoc />
        public void LineRight()
        {
            _scrollInfoImplementation?.LineRight();
        }

        /// <inheritdoc />
        public void LineUp()
        {
            _scrollInfoImplementation?.LineUp();
        }

        /// <inheritdoc />
        public Rect MakeVisible(Visual visual, Rect rectangle)
        {
            return _scrollInfoImplementation?.MakeVisible(visual, rectangle) ?? Rect.Empty;
        }

        /// <inheritdoc />
        public void MouseWheelDown()
        {
            _scrollInfoImplementation?.MouseWheelDown();
        }

        /// <inheritdoc />
        public void MouseWheelLeft()
        {
            _scrollInfoImplementation?.MouseWheelLeft();
        }

        /// <inheritdoc />
        public void MouseWheelRight()
        {
            _scrollInfoImplementation?.MouseWheelRight();
        }

        /// <inheritdoc />
        public void MouseWheelUp()
        {
            _scrollInfoImplementation?.MouseWheelUp();
        }

        /// <inheritdoc />
        public void PageDown()
        {
            _scrollInfoImplementation?.PageDown();
        }

        /// <inheritdoc />
        public void PageLeft()
        {
            _scrollInfoImplementation?.PageLeft();
        }

        /// <inheritdoc />
        public void PageRight()
        {
            _scrollInfoImplementation?.PageRight();
        }

        /// <inheritdoc />
        public void PageUp()
        {
            _scrollInfoImplementation?.PageUp();
        }

        /// <inheritdoc />
        public void SetHorizontalOffset(double offset)
        {
            _scrollInfoImplementation?.SetHorizontalOffset(offset);
        }

        /// <inheritdoc />
        public void SetVerticalOffset(double offset)
        {
            _scrollInfoImplementation?.SetVerticalOffset(offset);
        }

        /// <inheritdoc />
        public bool CanHorizontallyScroll
        {
            get { return _scrollInfoImplementation?.CanHorizontallyScroll ?? false; }
            set
            {
                if (_scrollInfoImplementation != null) _scrollInfoImplementation.CanHorizontallyScroll = value;
            }
        }

        /// <inheritdoc />
        public bool CanVerticallyScroll
        {
            get { return _scrollInfoImplementation?.CanVerticallyScroll ?? false; }
            set
            {
                if (_scrollInfoImplementation != null) _scrollInfoImplementation.CanVerticallyScroll = value;
            }
        }

        /// <inheritdoc />
        public double ExtentHeight
        {
            get { return _scrollInfoImplementation?.ExtentHeight ?? double.NaN; }
        }

        /// <inheritdoc />
        public double ExtentWidth
        {
            get { return _scrollInfoImplementation?.ExtentWidth ?? double.NaN; }
        }

        /// <inheritdoc />
        public double HorizontalOffset
        {
            get { return _scrollInfoImplementation?.HorizontalOffset ?? double.NaN; }
        }

        /// <inheritdoc />
        public ScrollViewer ScrollOwner
        {
            get { return _scrollInfoImplementation?.ScrollOwner ?? null!; }
            set
            {
                if (_scrollInfoImplementation != null) _scrollInfoImplementation.ScrollOwner = value;
            }
        }

        /// <inheritdoc />
        public double VerticalOffset
        {
            get { return _scrollInfoImplementation?.VerticalOffset ?? double.NaN; }
        }

        /// <inheritdoc />
        public double ViewportHeight
        {
            get { return _scrollInfoImplementation?.ViewportHeight ?? double.NaN; }
        }

        /// <inheritdoc />
        public double ViewportWidth
        {
            get { return _scrollInfoImplementation?.ViewportWidth ?? double.NaN; }
        }
#endif
    }
}