using System;
using System.Runtime.Serialization;
using JetBrains.Annotations;

namespace RoslynCodeControls
{
    public class CodeControlFaultException : CodeControlException
    {
        /// <inheritdoc />
        public CodeControlFaultException()
        {
        }

        /// <inheritdoc />
        protected CodeControlFaultException([NotNull] SerializationInfo info, StreamingContext context) : base(info,
            context)
        {
        }

        /// <inheritdoc />
        public CodeControlFaultException([CanBeNull] string message) : base(message)
        {
        }

        /// <inheritdoc />
        public CodeControlFaultException([CanBeNull] string message, [CanBeNull] Exception innerException) : base(
            message, innerException)
        {
        }
    }
}