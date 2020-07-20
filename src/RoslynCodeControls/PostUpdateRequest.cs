namespace RoslynCodeControls
{
    public class PostUpdateRequest
    {
        public PostUpdateInput Input { get; }

        public PostUpdateRequest(PostUpdateInput input)
        {
            Input = input;
        }

        public void Deconstruct(out PostUpdateInput input)
        {
            input = Input;
        }
    }
}