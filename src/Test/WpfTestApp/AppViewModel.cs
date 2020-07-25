using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Buildalyzer;
using Buildalyzer.Workspaces;
using JetBrains.Annotations;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.MSBuild;
using RoslynCodeControls;

namespace WpfTestApp
{
    public class AppViewModel : INotifyPropertyChanged
    {
        private Solution _solution;

        public async Task LoadFileOrProjectAsync(MainWindow mainWindow1, string filename)
        {
            if (filename != null && filename.EndsWith(".csproj"))
            {
                await LoadProjectAsync(mainWindow1, filename);
                return;
            }
            if (filename != null && filename.EndsWith(".sln"))
            {
                await LoadSolutionAsync(mainWindow1, filename);
                return;
            }

            var w = mainWindow1._workspace = new AdhocWorkspace(mainWindow1._host);
            w.AddSolution(SolutionInfo.Create(SolutionId.CreateNewId(), VersionStamp.Create()));
            var projectInfo = ProjectInfo.Create(ProjectId.CreateNewId(), VersionStamp.Create(),
                "Code Project", "code", LanguageNames.CSharp);
            var w2 = w.CurrentSolution.AddProject(projectInfo);
            w.TryApplyChanges(w2);

            DocumentInfo documentInfo;
            if (filename != null)
                documentInfo = DocumentInfo.Create(DocumentId.CreateNewId(projectInfo.Id), "Default",
                    null, SourceCodeKind.Regular, new FileTextLoader(filename, Encoding.UTF8), filename);
            else
                documentInfo = DocumentInfo.Create(DocumentId.CreateNewId(projectInfo.Id), "Default",
                    null, SourceCodeKind.Regular);

            w2 = w.CurrentSolution.AddDocument(documentInfo);
            w.TryApplyChanges(w2);

            mainWindow1.Project = w.CurrentSolution.GetProject(projectInfo.Id);
            mainWindow1.Document = w.CurrentSolution.GetDocument(documentInfo.Id);
        }

        private async Task LoadSolutionAsync(MainWindow mainWindow1, string filename)
        {
            SolutionFilePath = filename;
            var ww = CreateMsBuildWorkspace();
            var solution = await ww.OpenSolutionAsync(filename, new Progress1(mainWindow1)).ConfigureAwait(true);
            Solution = solution;


        }

        public Solution Solution
        {
            get { return _solution; }
            set
            {
                if (Equals(value, _solution)) return;
                _solution = value;
                OnPropertyChanged();
            }
        }

        public static async Task LoadProjectAsync(MainWindow mainWindow1, string s)
        {
            // StatusScrollViewer.Visibility = Visibility.Visible;
            // status.Visibility = Visibility.Visible;

            var ww = CreateMsBuildWorkspace();
            var project = await ww.OpenProjectAsync(s, new Progress1(mainWindow1)).ConfigureAwait(true);
            mainWindow1.Project = project;
            foreach (var projectAnalyzerReference in project.AnalyzerReferences)
            {
                Debug.WriteLine("Existing analyzer reference " + projectAnalyzerReference);
            }
            foreach (var mainWindow1AnalyzerDll in mainWindow1.AnalyzerDlls)
            {
                mainWindow1.LoadAnalyzers(null, mainWindow1AnalyzerDll);
            }

            project = mainWindow1.Project;
            foreach (var d in project.Documents)
            {
                var tree = await d.GetSyntaxTreeAsync();
                var model = await d.GetSemanticModelAsync();
                foreach (var (item1, item2, item3) in mainWindow1.AnalyzerContexts.SelectMany(z => z.SyntaxNodeActions))
                {
                    if (item2 == typeof(SyntaxKind))
                    {
                        Debug.WriteLine(item2);
                        foreach (SyntaxKind o in item3)
                        {
                            var nodes = tree.GetRoot().DescendantNodesAndSelf().Where(n => n.Kind() == o);
                            foreach (var syntaxNode in nodes)
                            {
                                var ctx = new SyntaxNodeAnalysisContext(syntaxNode, model,
                                    new AnalyzerOptions(ImmutableArray<AdditionalText>.Empty),
                                    diagnostic =>
                                    {

                                        var diagnosticLocation = diagnostic.Location.SourceSpan.ToString();
                                        var message =
                                            $"{diagnostic.Descriptor.Description}  {diagnostic.Severity}  {diagnostic.Id}  {diagnosticLocation}  {diagnostic.GetMessage()}";


                                        mainWindow1.status.Text += message + "\r\n\r\n";

                                        mainWindow1.StatusScrollViewer.ScrollToBottom();

                                    },
                                    tru => true,
                                    CancellationToken.None);
                                try
                                {
                                    item1(ctx);
                                }
                                catch
                                {

                                }
                            }

                            Debug.WriteLine(o);
                        }
                    }
                }
            }

            mainWindow1.StatusScrollViewer.Visibility = Visibility.Hidden;
            mainWindow1.status.Visibility = Visibility.Hidden;

        }

        private static MSBuildWorkspace CreateMsBuildWorkspace()
        {
            return MSBuildWorkspace.Create();
        }

        public IEnumerable<Diagnostic> RunAnalyzers(EnhancedCodeControl enhancedCodeControl, List<MyAnalyzerContext> myAnalyzerContexts, SyntaxTree tree, SemanticModel model)
        {
            List<Diagnostic> reported = new List<Diagnostic>();
            foreach (var action in myAnalyzerContexts.SelectMany(z => z.SyntaxTreeActions))
            {
                var ct = new SyntaxTreeAnalysisContext(enhancedCodeControl.SyntaxTree,
                    new AnalyzerOptions(ImmutableArray<AdditionalText>.Empty),
                    diagnostic =>
                    {
                        Debug.WriteLine(diagnostic.GetMessage());
                        reported.Add(diagnostic);
                    }, diagnostic => true, CancellationToken.None);
                action(ct);
            }


            foreach (var (item1, item2, item3) in myAnalyzerContexts.SelectMany(z => z.SyntaxNodeActions))
            {
                if (item2 != typeof(SyntaxKind)) continue;
                Debug.WriteLine(item2);
                foreach (SyntaxKind o in item3)
                {
                    var nodes = tree.GetRoot().DescendantNodesAndSelf().Where(n => n.Kind() == o);
                    foreach (var syntaxNode in nodes)
                    {
                        var ctx = new SyntaxNodeAnalysisContext(syntaxNode, model,
                            new AnalyzerOptions(ImmutableArray<AdditionalText>.Empty),
                            diagnostic =>
                            {
                                reported.Add(diagnostic);
                                var diagnosticLocation = diagnostic.Location.SourceSpan.ToString();
                                var message =
                                    $"{diagnostic.Descriptor.Description}  {diagnostic.Severity}  {diagnostic.Id}  {diagnosticLocation}  {diagnostic.GetMessage()}";
                                Debug.WriteLine(
                                    message);
                            },
                            tru => true,
                            CancellationToken.None);
                        item1(ctx);
                    }

                    Debug.WriteLine(o);
                }
            }

            return reported;
        }

        public  async Task CompileAsync(Project project, MainWindow mainWindow)
        {
            IAnalyzerManager m = new AnalyzerManager(SolutionFilePath);
            // var analyzer = m.GetProject(project.FilePath);
            // var x =  m.GetWorkspace();
            // AdhocWorkspace workspace = analyzer.GetWorkspace();
            foreach (var (key, value) in m.Projects)
            {
                Debug.WriteLine(key);
            }

            var p = m.Projects[project.FilePath];
            
            var results = p.Build();
            AdhocWorkspace w=null;
            foreach (var analyzerResult in results)
            {
                w = analyzerResult.GetWorkspace();
                break;
            }

            mainWindow.Project = w.CurrentSolution.Projects.First(z => z.FilePath == project.FilePath);
            foreach (var mainWindowAnalyzerDll in mainWindow.AnalyzerDlls)
            {
                mainWindow.LoadAnalyzers(null,mainWindowAnalyzerDll);
            }

            var curProject = mainWindow.Project;
            // var w = 
            // foreach (var action in curProject.SelectMany(z => z.CompilationStartActions))
            // {
            //     var ct = new MyComp(Compilation,
            //         new AnalyzerOptions(ImmutableArray<AdditionalText>.Empty), CancellationToken.None);
            //     action(ct);
         
            // }
            foreach (var projectAllProjectReference in curProject.AllProjectReferences)
            {
                Debug.WriteLine("Project reference " + projectAllProjectReference);
            }

            foreach (var r in curProject.MetadataReferences)
            {
                Debug.WriteLine("metadata reference " + r.Display);
            }

            if (curProject.AnalyzerReferences != null)
                foreach (var projectAnalyzerReference in curProject.AnalyzerReferences)
                {
                    Debug.WriteLine("analyzer reference " + projectAnalyzerReference.Display);
                }

            
            var compilation = await curProject.GetCompilationAsync();
            foreach (var diagnostic in compilation.GetDiagnostics())
            {
                if (diagnostic.IsSuppressed)
                    continue;
                if (diagnostic.Severity < DiagnosticSeverity.Error)
                    continue;
                Debug.WriteLine("diag " + diagnostic.GetMessage());
            }
            foreach (var diagnostic in compilation.GetDiagnostics())
            {
                if (diagnostic.IsSuppressed)
                    continue;
                if (diagnostic.Severity >= DiagnosticSeverity.Error)
                    continue;
                if (diagnostic.Severity < DiagnosticSeverity.Info)
                    continue;

                Debug.WriteLine("diag " + diagnostic.GetMessage());
            }
        }

        public  string SolutionFilePath { get; set; }

        public Compilation Compilation { get; set; }
        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}