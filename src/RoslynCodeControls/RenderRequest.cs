    namespace RoslynCodeControls
{
    public readonly struct RenderRequest
    {
        public InputRequest InputRequest { get; }
        public int InsertionPoint { get;  }
        public RenderRequestInput Input { get; }

        public RenderRequest(InputRequest inputRequest, int insertionPoint, RenderRequestInput input)
        {
            InputRequest = inputRequest;
            InsertionPoint = insertionPoint;
            Input = input;
        }
    }
}