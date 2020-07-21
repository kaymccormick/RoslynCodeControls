using System;

namespace RoslynCodeControls
{
    public readonly struct UpdateComplete : ITimestampedRequest
    {
        public InputRequest InputRequest { get; }
        public int NewInsertionPoint { get; }
        public DateTime PostUpdateTimestamp { get; }
        public DateTime RenderRequestTimestamp { get; }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"{nameof(InputRequest)}: {InputRequest}, {nameof(NewInsertionPoint)}: {NewInsertionPoint}";
        }

        public UpdateComplete(InputRequest inputRequest, int newInsertionPoint, DateTime postUpdateTimestamp,
            DateTime rRenderRequestTimestamp)
        {
            InputRequest = inputRequest;
            NewInsertionPoint = newInsertionPoint;
            PostUpdateTimestamp = postUpdateTimestamp;
            RenderRequestTimestamp = rRenderRequestTimestamp;
            Timestamp = DateTime.Now;
            IsSuccess = true;

        }

        /// <inheritdoc />
        public DateTime Timestamp { get; }

        public bool IsSuccess { get;  }
    }
}