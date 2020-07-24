using System;
using System.Runtime.Serialization;
using JetBrains.Annotations;

namespace RoslynCodeControls
{
    public class CodeControlException : Exception
    {
        /// <inheritdoc />
        public CodeControlException()
        {
        }

        /// <inheritdoc />
        protected CodeControlException([NotNull] SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }

        /// <inheritdoc />
        public CodeControlException([CanBeNull] string message) : base(message)
        {
        }

        /// <inheritdoc />
        public CodeControlException([CanBeNull] string message, [CanBeNull] Exception innerException) : base(message, innerException)
        {
        }
    }
}