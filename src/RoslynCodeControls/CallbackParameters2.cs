using System.Windows.Media;

namespace RoslynCodeControls
{
    public class CallbackParameters2
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

        public CallbackParameters2(RoslynCodeControl roslynCodeControl, in int insertionPoint,
            InputRequest inputRequest,
            string text, CallbackParameters1 in1, LineInfo2 lineInfo, DrawingGroup drawingGroup, double maxX,
            double maxY)
        {
            RoslynCodeControl = roslynCodeControl;
            InsertionPoint = insertionPoint;
            InputRequest = inputRequest;
            Text = text;
            In1 = in1;
            LineInfo = lineInfo;
            DrawingGroup = drawingGroup;
            MaxX = maxX;
            MaxY = maxY;
        }
    }
}