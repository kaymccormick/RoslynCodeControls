using System;
using Microsoft.CodeAnalysis.Text;

namespace RoslynCodeControls
{
    public readonly struct UpdateComplete : ITimestampedRequest
    {
        public InputRequest InputRequest { get; }
        public int NewInsertionPoint { get; }
        public TextChange? Change { get; }
        public DateTime PostUpdateTimestamp { get; }
        public DateTime RenderRequestTimestamp { get; }
        public DateTime RenderBeganTimestamp { get; }
        public DateTime RenderCompleteTimestamp { get; }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"{nameof(InputRequest)}: {InputRequest}, {nameof(NewInsertionPoint)}: {NewInsertionPoint}";
        }

        public UpdateComplete(InputRequest inputRequest, int newInsertionPoint, TextChange? change,
            DateTime postUpdateTimestamp,
            DateTime rRenderRequestTimestamp, DateTime renderBeganTimestamp, DateTime renderCompleteTimestamp)
        {
            InputRequest = inputRequest;
            NewInsertionPoint = newInsertionPoint;
            Change = change;
            PostUpdateTimestamp = postUpdateTimestamp;
            RenderRequestTimestamp = rRenderRequestTimestamp;
            RenderBeganTimestamp = renderBeganTimestamp;
            RenderCompleteTimestamp = renderCompleteTimestamp;
            Timestamp = DateTime.Now;
            IsSuccess = true;

        }

        /// <inheritdoc />
        public DateTime Timestamp { get; }

        public bool IsSuccess { get;  }
    }
}