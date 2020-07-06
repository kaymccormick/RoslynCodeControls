namespace RoslynCodeControls
{
    internal class CallbackParameters2
    {
        public RoslynCodeControl RoslynCodeControl { get; }
        public int InsertionPoint { get; }
        public InputRequest InputRequest { get; }
        public string Text { get; }
        public InClassName In1 { get; }
        public LineInfo2 LineInfo { get; }

        public CallbackParameters2(RoslynCodeControl roslynCodeControl, in int insertionPoint, InputRequest inputRequest,
            string text, InClassName in1, LineInfo2 lineInfo)
        {
            RoslynCodeControl = roslynCodeControl;
            InsertionPoint = insertionPoint;
            InputRequest = inputRequest;
            Text = text;
            In1 = in1;
            LineInfo = lineInfo;
        }
    }
}