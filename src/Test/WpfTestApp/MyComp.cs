using System;
using System.Collections.Immutable;
using System.Threading;
using JetBrains.Annotations;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace WpfTestApp
{
    public class MyComp : CompilationStartAnalysisContext
    {
        [NotNull] public AnalyzerOptions Options { get; }

        /// <inheritdoc />
        public MyComp([NotNull] Compilation compilation, [NotNull] AnalyzerOptions options, CancellationToken cancellationToken) : base(compilation, options, cancellationToken)
        {
            Options = options;
        }

        /// <inheritdoc />
        public override void RegisterCompilationEndAction(Action<CompilationAnalysisContext> action)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public override void RegisterSemanticModelAction(Action<SemanticModelAnalysisContext> action)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public override void RegisterSymbolAction(Action<SymbolAnalysisContext> action, ImmutableArray<SymbolKind> symbolKinds)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public override void RegisterCodeBlockStartAction<TLanguageKindEnum>(Action<CodeBlockStartAnalysisContext<TLanguageKindEnum>> action)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public override void RegisterCodeBlockAction(Action<CodeBlockAnalysisContext> action)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public override void RegisterSyntaxTreeAction(Action<SyntaxTreeAnalysisContext> action)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public override void RegisterSyntaxNodeAction<TLanguageKindEnum>(Action<SyntaxNodeAnalysisContext> action, ImmutableArray<TLanguageKindEnum> syntaxKinds)
        {
            throw new NotImplementedException();
        }
    }
}