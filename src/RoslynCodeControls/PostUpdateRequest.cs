using System;

namespace RoslynCodeControls
{
    public readonly struct PostUpdateRequest
    {
        public PostUpdateInput Input { get; }
        public DateTime RenderRequestTimestamp { get; }

        public PostUpdateRequest(PostUpdateInput input, DateTime renderRequestTimestamp)
        {
            Input = input;
            RenderRequestTimestamp = renderRequestTimestamp;
        }

        public void Deconstruct(out PostUpdateInput input)
        {
            input = Input;
        }
    }
}