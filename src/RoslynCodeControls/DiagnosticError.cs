using Microsoft.CodeAnalysis;

namespace RoslynCodeControls
{
    /// <summary>
    /// 
    /// </summary>
    public class DiagnosticError : CompilationError
    {
        private readonly Diagnostic _diagnostic;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="diagnostic"></param>
        public DiagnosticError(Diagnostic diagnostic)
        {
            _diagnostic = diagnostic;
            Message = _diagnostic.GetMessage();
        }
    }
}