    namespace RoslynCodeControls
{
    public class RenderRequest
    {
        public InputRequest InputRequest { get; }
        public int InsertionPoint { get;  }
        public CallbackParameters1 Inn { get; }

        public RenderRequest(InputRequest inputRequest, int insertionPoint, CallbackParameters1 inn)
        {
            InputRequest = inputRequest;
            InsertionPoint = insertionPoint;
            Inn = inn;
        }
    }
}