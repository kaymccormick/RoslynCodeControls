using System.Collections.Generic;
using System.Windows.Media;

namespace RoslynCodeControls
{
    public class RedrawLineResult 
    {
        public LineInfo2 LineInfo { get; }
        public DrawingGroup DrawingGroup { get; }
        public double LineMaxX { get; }
        public double LineMaxY { get; }
        public LinkedList<CharInfo> CharInfos { get; }
        public List<TextRunInfo> RunsInfos { get; }

        public RedrawLineResult(LineInfo2 lineInfo, DrawingGroup drawingGroup, in double lineMaxX, in double lineMaxY,
            LinkedList<CharInfo> charInfos, List<TextRunInfo> runsInfos)
        {
            LineInfo = lineInfo;
            DrawingGroup = drawingGroup;
            LineMaxX = lineMaxX;
            LineMaxY = lineMaxY;
            CharInfos = charInfos;
            RunsInfos = runsInfos;
        }
    }
}