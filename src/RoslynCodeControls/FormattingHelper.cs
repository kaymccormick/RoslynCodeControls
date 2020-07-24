using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.TextFormatting;

namespace RoslynCodeControls
{
    /// <summary>
    /// 
    /// </summary>
    public static class FormattingHelper
    {
        private static TextFormatter Formatter = TextFormatter.Create();


        private static DrawingGroup SaveDrawingGroup(LineContext lineContext)
        {
            var dd = new DrawingGroup();
            var dc1 = dd.Open();
            dc1.DrawRectangle(Brushes.White, null,
                new Rect(0, 0, lineContext.MyTextLine.WidthIncludingTrailingWhitespace, lineContext.MyTextLine.Height));
            lineContext.MyTextLine.Draw(dc1, new Point(0, 0), InvertAxes.None);
            dc1.Close();
            var imgWidth = (int) dd.Bounds.Width;
            var imgHeight = (int) dd.Bounds.Height;
            if (imgWidth > 0 && imgHeight > 0)
                SaveImage(dd, lineContext.LineNumber.ToString(),
                    imgWidth, imgHeight);
            return dd;
        }

        private static void SaveImage(DrawingGroup drawingGroup,
            string filePrefix, int width, int height)
        {
            Debug.WriteLine("Creating image " + $"({width},{height}) {filePrefix}.png");
            var v = new DrawingVisual();
            var dc = v.RenderOpen();
            var bounds = drawingGroup.Bounds;

            var brush = new DrawingBrush(drawingGroup);
            dc.DrawRectangle(
                brush, null, bounds);
            dc.Close();
            var rtb = new RenderTargetBitmap(width, height, 96,
                96,
                PixelFormats.Pbgra32);
            rtb.Render(v);

            var png = new PngBitmapEncoder();
            png.Frames.Add(BitmapFrame.Create(rtb));
            var fname = $"{filePrefix}.png";
            using (var s = File.Create("C:\\temp\\" + fname))
            {
                png.Save(s);
            }
        }

    }
}