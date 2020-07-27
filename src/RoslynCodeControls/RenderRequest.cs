    using System;

    namespace RoslynCodeControls
{
    public readonly struct RenderRequest : ITimestampedRequest
    {
        public InputRequest InputRequest { get; }
        public int InsertionPoint { get;  }
        public RenderRequestInput Input { get; }
        public LineInfo2? LineInfo { get; }

        public RenderRequest(InputRequest inputRequest, int insertionPoint, RenderRequestInput input, LineInfo2? lineInfo)
        {
            InputRequest = inputRequest;
            InsertionPoint = insertionPoint;
            Input = input;
            LineInfo = lineInfo;
            Timestamp = DateTime.Now;
        }

        /// <inheritdoc />
        public DateTime Timestamp { get; }
    }
}