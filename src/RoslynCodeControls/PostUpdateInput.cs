using System.Windows.Media;

namespace RoslynCodeControls
{
    public class PostUpdateInput
    {
        public RoslynCodeControl RoslynCodeControl { get; }
        public int InsertionPoint { get; }
        public InputRequest InputRequest { get; }
        public string Text { get; }
        public CallbackParameters1 In1 { get; }
        public LineInfo2 LineInfo { get; }
        public DrawingGroup DrawingGroup { get; }
        public double MaxX { get; }
        public double MaxY { get; }
        public RedrawLineResult RedrawLineResult { get; }

        public PostUpdateInput(RoslynCodeControl roslynCodeControl, in int insertionPoint,
            InputRequest inputRequest,
            string text, CallbackParameters1 in1, RedrawLineResult redrawLineResult)
        {
            RoslynCodeControl = roslynCodeControl;
            InsertionPoint = insertionPoint;
            InputRequest = inputRequest;
            Text = text;
            In1 = in1;
            LineInfo = redrawLineResult.LineInfo;
            DrawingGroup = redrawLineResult.DrawingGroup;
            MaxX = redrawLineResult.LineMaxX;
            MaxY = redrawLineResult.LineMaxY;
            RedrawLineResult = redrawLineResult;
        }
    }
}