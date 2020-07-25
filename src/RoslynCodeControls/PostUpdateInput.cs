using System;
using System.Windows.Media;
using Microsoft.CodeAnalysis.Text;

namespace RoslynCodeControls
{
    public readonly struct PostUpdateInput : ITimestampedRequest
    {
        public RoslynCodeControl RoslynCodeControl { get; }
        public int InsertionPoint { get; }
        public InputRequest InputRequest { get; }
        public LineInfo2 LineInfo { get; }
        public RedrawLineResult RedrawLineResult { get; }
        public TextChange? Change { get; }

        public PostUpdateInput(RoslynCodeControl roslynCodeControl, in int insertionPoint,
            InputRequest inputRequest, RedrawLineResult redrawLineResult, TextChange? change)
        {
            RoslynCodeControl = roslynCodeControl;
            InsertionPoint = insertionPoint;
            InputRequest = inputRequest;
            LineInfo = redrawLineResult.LineInfo;
            RedrawLineResult = redrawLineResult;
            Change = change;
            Timestamp = DateTime.Now;
        }

        /// <inheritdoc />
        public DateTime Timestamp { get; }
    }
}