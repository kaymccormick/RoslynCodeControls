namespace RoslynCodeControls
{
    public class PostUpdateRequest
    {
        public CallbackParameters2 In2 { get; }
        public CallbackParameters1 Inn { get; }

        public PostUpdateRequest(CallbackParameters2 in2, CallbackParameters1 inn)
        {
            In2 = in2;
            Inn = inn;
        }
    }
}