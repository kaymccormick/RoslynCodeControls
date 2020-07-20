﻿namespace RoslynCodeControls
{
    public readonly struct PostUpdateRequest
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