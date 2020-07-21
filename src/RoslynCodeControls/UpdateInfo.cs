using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace RoslynCodeControls
{
    public class UpdateInfo
    {
        public BitmapSource ImageSource { get; set; }
        public Rect Rect { get; set; }
        public DrawingGroup DrawingGroup { get; set; }
        public List<CharInfo> CharInfos { get; set; }
        public bool FinalBlock { get; set; }
        public LineInfo2 LineInfo { get; set; }
    }
}