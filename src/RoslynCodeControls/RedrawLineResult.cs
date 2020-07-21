using System;
using System.Collections.Generic;
using System.Windows.Media;

namespace RoslynCodeControls
{
    public readonly struct RedrawLineResult : ITimestampedRequest
    {
        public LineInfo2 LineInfo { get; }
        public DrawingGroup DrawingGroup { get; }
        public double LineMaxX { get; }
        public double LineMaxY { get; }
        public LinkedList<CharInfo> CharInfos { get; }
        public List<TextRunInfo> RunsInfos { get; }
        public bool IsNewLineInfo { get; }
        public DateTime BeganTimestamp { get; }

        public RedrawLineResult(LineInfo2 lineInfo, DrawingGroup drawingGroup, in double lineMaxX, in double lineMaxY,
            LinkedList<CharInfo> charInfos, List<TextRunInfo> runsInfos, bool newLineInfo, DateTime beganTimestamp)
        {
            LineInfo = lineInfo;
            DrawingGroup = drawingGroup;
            LineMaxX = lineMaxX;
            LineMaxY = lineMaxY;
            CharInfos = charInfos;
            RunsInfos = runsInfos;
            IsNewLineInfo = newLineInfo;
            BeganTimestamp = beganTimestamp;
            Timestamp = DateTime.Now;
            
        }

        /// <inheritdoc />
        public DateTime Timestamp { get; }
    }
}