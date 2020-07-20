using System.Windows.Media;

namespace RoslynCodeControls
{
    public readonly struct PostUpdateInput
    {
        public RoslynCodeControl RoslynCodeControl { get; }
        public int InsertionPoint { get; }
        public InputRequest InputRequest { get; }
        public LineInfo2 LineInfo { get; }
        public RedrawLineResult RedrawLineResult { get; }

        public PostUpdateInput(RoslynCodeControl roslynCodeControl, in int insertionPoint,
            InputRequest inputRequest, RedrawLineResult redrawLineResult)
        {
            RoslynCodeControl = roslynCodeControl;
            InsertionPoint = insertionPoint;
            InputRequest = inputRequest;
            LineInfo = redrawLineResult.LineInfo;
            RedrawLineResult = redrawLineResult;
        }
    }
}