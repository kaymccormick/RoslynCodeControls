using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace RoslynCodeControls
{
    public readonly struct UpdateInfo
    {
        public UpdateInfo(DrawingGroup myGroup, in int liIndex, LineInfo2[] lineInfos)
        {
            DrawingGroup = myGroup;
            NumLineInfos = liIndex;
            LineInfos = lineInfos;
        }

        public DrawingGroup DrawingGroup { get;  }
        public LineInfo2[] LineInfos { get;  }
        public int NumLineInfos { get;  }
    }
}