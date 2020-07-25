using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace WpfTestApp
{
    public class MyAnalyzerContext : AnalysisContext
    {
        public DiagnosticAnalyzer Analyzer { get; }

        /// <inheritdoc />
        public MyAnalyzerContext(DiagnosticAnalyzer analyzer)
        {
            Analyzer = analyzer;
        }

        /// <inheritdoc />
        public override void RegisterSymbolStartAction(Action<SymbolStartAnalysisContext> action, SymbolKind symbolKind)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public override void RegisterOperationBlockStartAction(Action<OperationBlockStartAnalysisContext> action)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public override void RegisterOperationBlockAction(Action<OperationBlockAnalysisContext> action)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public override void RegisterOperationAction(Action<OperationAnalysisContext> action, ImmutableArray<OperationKind> operationKinds)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public override void EnableConcurrentExecution()
        {
            ConcurrentExetion = true;
            Debug.WriteLine(nameof(EnableConcurrentExecution));
        }

        public bool ConcurrentExetion { get; set; }

        /// <inheritdoc />
        public override void ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags analysisMode)
        {
            GeneratedAnalysisMode = analysisMode;
        }

        public GeneratedCodeAnalysisFlags GeneratedAnalysisMode { get; set; }

        /// <inheritdoc />
        public override void RegisterCompilationStartAction(Action<CompilationStartAnalysisContext> action)
        {
            Debug.WriteLine(nameof(RegisterCompilationStartAction));
            CompilationStartActions.Add(action);

        }

        public List<Action<CompilationStartAnalysisContext>> CompilationStartActions { get; set; } = new List<Action<CompilationStartAnalysisContext>>();

        /// <inheritdoc />
        public override void RegisterCompilationAction(Action<CompilationAnalysisContext> action)
        {
            Debug.WriteLine(nameof(RegisterCompilationAction));
            throw new NotImplementedException();
        }

        /// <inheritdoc />nameof(
        public override void RegisterSemanticModelAction(Action<SemanticModelAnalysisContext> action)
        {
            Debug.WriteLine(nameof(RegisterSemanticModelAction));
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public override void RegisterSymbolAction(Action<SymbolAnalysisContext> action, ImmutableArray<SymbolKind> symbolKinds)
        {
            Debug.WriteLine(nameof(RegisterSymbolAction));
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public override void RegisterCodeBlockStartAction<TLanguageKindEnum>(Action<CodeBlockStartAnalysisContext<TLanguageKindEnum>> action)
        {
            Debug.WriteLine(nameof(RegisterCodeBlockStartAction));
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public override void RegisterCodeBlockAction(Action<CodeBlockAnalysisContext> action)
        {
            Debug.WriteLine(nameof(RegisterCodeBlockAction));
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public override void RegisterSyntaxTreeAction(Action<SyntaxTreeAnalysisContext> action)
        {
            Debug.WriteLine(nameof(RegisterSyntaxTreeAction));
            SyntaxTreeActions.Add(action);
        }

        public List<Action<SyntaxTreeAnalysisContext>> SyntaxTreeActions { get; } = new List<Action<SyntaxTreeAnalysisContext>>();

        /// <inheritdoc />
        public override void RegisterSyntaxNodeAction<TLanguageKindEnum>(Action<SyntaxNodeAnalysisContext> action, ImmutableArray<TLanguageKindEnum> syntaxKinds)
        {
            Debug.WriteLine(nameof(RegisterSyntaxNodeAction));
            SyntaxNodeActions.Add(Tuple.Create(action, typeof(TLanguageKindEnum), (IList)syntaxKinds));
        }

        public List< Tuple< Action<SyntaxNodeAnalysisContext>, Type,IList >> SyntaxNodeActions  { get; } = new List<Tuple<Action<SyntaxNodeAnalysisContext>, Type, IList>>();
    }
}