using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using JetBrains.Annotations;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.VisualStudio.Threading;
using Microsoft.Win32;
using RoslynCodeControls;

using Path = System.IO.Path;
namespace WpfTestApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public static RoutedUICommand HideToolBar =
            new RoutedUICommand("Hide toolbar", nameof(HideToolBar), typeof(MainWindow));

        public static readonly DependencyProperty ViewModelProperty = DependencyProperty.Register(
            "ViewModel", typeof(AppViewModel), typeof(MainWindow), new PropertyMetadata(default(AppViewModel), OnViewModelChanged));

        public AppViewModel ViewModel
        {
            get { return (AppViewModel) GetValue(ViewModelProperty); }
            set { SetValue(ViewModelProperty, value); }
        }

        private static void OnViewModelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((MainWindow) d).OnViewModelChanged((AppViewModel) e.OldValue, (AppViewModel) e.NewValue);
        }



        protected virtual void OnViewModelChanged(AppViewModel oldValue, AppViewModel newValue)
        {
        }

        public static double[] CommonFontSizes
        {
            get
            {
                return new double[]
                {
                    3.0d, 4.0d, 5.0d, 6.0d, 6.5d, 7.0d, 7.5d, 8.0d, 8.5d, 9.0d,
                    9.5d, 10.0d, 10.5d, 11.0d, 11.5d, 12.0d, 12.5d, 13.0d, 13.5d, 14.0d,
                    15.0d, 16.0d, 17.0d, 18.0d, 19.0d, 20.0d, 22.0d, 24.0d, 26.0d, 28.0d,
                    30.0d, 32.0d, 34.0d, 36.0d, 38.0d, 40.0d, 44.0d, 48.0d, 52.0d, 56.0d,
                    60.0d, 64.0d, 68.0d, 72.0d, 76.0d, 80.0d, 88.0d, 96.0d, 104.0d, 112.0d,
                    120.0d, 128.0d, 136.0d, 144.0d, 152.0d, 160.0d, 176.0d, 192.0d, 208.0d,
                    224.0d, 240.0d, 256.0d, 272.0d, 288.0d, 304.0d, 320.0d, 352.0d, 384.0d,
                    416.0d, 448.0d, 480.0d, 512.0d, 544.0d, 576.0d, 608.0d, 640.0d
                };
            }
        }


        public static readonly DependencyProperty FontsProperty = DependencyProperty.Register(
            "Fonts", typeof(IEnumerable), typeof(MainWindow),
            new PropertyMetadata(default(IEnumerable), null, CoerceFontsValue));

        private static object CoerceFontsValue(DependencyObject d, object basevalue)
        {
            return System.Windows.Media.Fonts.SystemFontFamilies;
        }

        public IEnumerable Fonts
        {
            get { return (IEnumerable) GetValue(FontsProperty); }
            set { SetValue(FontsProperty, value); }
        }

        public static readonly DependencyProperty DefaultHideToolBarCommandProperty = DependencyProperty.Register(
            "DefaultHideToolBarCommand", typeof(ICommand), typeof(MainWindow), new PropertyMetadata(HideToolBar));

        private AdhocWorkspace _workspace;

        public ICommand DefaultHideToolBarCommand
        {
            get { return (ICommand) GetValue(DefaultHideToolBarCommandProperty); }
            set { SetValue(DefaultHideToolBarCommandProperty, value); }
        }

        public JoinableTaskFactory JTF2 { get; set; }

        public MainWindow()
        {
            var tmp = System.IO.Path.GetTempFileName();
            File.Delete(tmp);
            Directory.CreateDirectory(tmp);
            WorkDir = tmp;
            AnalyzersDir = Path.Combine(WorkDir, "analyzers");
            Directory.CreateDirectory(AnalyzersDir);
            InitializeComponent();
            CoerceValue(FontsProperty);
            Loaded += OnLoaded;
            LoadAnalyzers(@"C:\temp\roslyn.analyzers.dll");

            CommandBindings.Add(new CommandBinding(HideToolBar, OnExecutedHideToolBar));
        }

        public string AnalyzersDir { get; set; }

        public string WorkDir { get; set; }

        private void LoadAnalyzers(string dllFile)
        {
            var analyzersAssembly  = Assembly.LoadFrom(dllFile);
            var analyzerTypes = analyzersAssembly.ExportedTypes.Where(t => typeof(DiagnosticAnalyzer).IsAssignableFrom(t) &&  t.GetCustomAttribute<DiagnosticAnalyzerAttribute>() != null).ToList();
            foreach (var analyzerType in analyzerTypes)
            {
                Debug.WriteLine(analyzerType);
                var  z= (DiagnosticAnalyzer)Activator.CreateInstance(analyzerType);
                var c = new MyAnalyzerContext(z);
                z.Initialize(c);
                AnalyzerContexts.Add(c);

            }
        }

        public List<MyAnalyzerContext> AnalyzerContexts { get; set; } = new List<MyAnalyzerContext>();

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            JTF.RunAsync(M1);
        }

        private async Task M1()
        {
            Thread.CurrentThread.Name = "App thread";
            _host = MefHostServices.Create(MefHostServices.DefaultAssemblies);

            await LoadFileOrProjectAsync();
#if false
            var project = _workspace.AddProject("Default project", LanguageNames.CSharp);
            var documentId = DocumentId.CreateNewId(project.Id);
            Document doocument;
            if (Filename != null)
            {
                var documentInfo = DocumentInfo.Create(documentId,
                    "default.cs",
                    null, SourceCodeKind.Regular, new FileTextLoader(Filename, Encoding.UTF8), Filename);
                doocument = _workspace.AddDocument(documentInfo);
            }
            else
            {
                var documentInfo = DocumentInfo.Create(documentId,
                    "default.cs");
                doocument = _workspace.AddDocument(documentInfo);
            }
#endif
            await SetupCodeControlAsync();

            StartupCommmad?.Execute(null);
        }

        private async Task SetupCodeControlAsync()
        {
            // CodeControl.Workspace = _workspace;
            CodeControl.DebugLevel = 2;
            CodeControl.JTF2 = JTF2;
            CodeControl.Document = Document;
            var tree = await Document.GetSyntaxTreeAsync();
            CodeControl.SyntaxTree = tree;
            var semanticModelAsync = await Document.GetSemanticModelAsync();
            CodeControl.SemanticModel = semanticModelAsync;
            if (semanticModelAsync != null) CodeControl.Compilation = semanticModelAsync.Compilation;

            CodeControl.AddHandler(RoslynCodeControl.ContentChangedEvent, new RoslynCodeControl.ContentChangedRoutedEventHandler(CodeControlContentnChanged));
            CodeControl.AddHandler(RoslynCodeBase.RenderStartEvent, new RoutedEventHandler((sender, args) =>
            {
                StartTime = DateTime.Now;
                Debug.WriteLine("render start");
            }));
            CodeControl.AddHandler(RoslynCodeBase.RenderCompleteEvent, new RoutedEventHandler((sender, args) =>
            {
                var span = DateTime.Now - StartTime;
                Debug.WriteLine("render complete " + span);
            }));
            await CodeControl.UpdateFormattedTextAsync();

            var d = RunAnalyzers(CodeControl, AnalyzerContexts, tree, semanticModelAsync);
            Diagnostics = d;
        }

        private static List<Diagnostic> RunAnalyzers(EnhancedCodeControl enhancedCodeControl, List<MyAnalyzerContext> myAnalyzerContexts, SyntaxTree tree, SemanticModel model)
        {
            List<Diagnostic> reported = new List<Diagnostic>();
            foreach (var action in myAnalyzerContexts.SelectMany(z => z.SyntaxTreeActions))
            {
                SyntaxTreeAnalysisContext ct = new SyntaxTreeAnalysisContext(enhancedCodeControl.SyntaxTree,
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

        private void CodeControlContentnChanged(object sender, ContentChangedRoutedEventArgs e)
        {
            
            var c = (RoslynCodeControl)e.OriginalSource;
            var tree = c.SyntaxTree;
            var model = c.SemanticModel;
            var reported = RunAnalyzers(CodeControl, AnalyzerContexts, tree, model);
            Diagnostics = reported;

        }

        private DateTime StartTime { get; set; }

        private JoinableTaskFactory JTF { get; set; } = new JoinableTaskFactory(new JoinableTaskContext());
        public string Filename { get; set; }
        public ICommand StartupCommmad { get; set; }

        private async void FontComboBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            CodeControl.FontFamily = (FontFamily) FontComboBox.SelectedItem;
            // await CodeControl.UpdateTextSourceAsync();
        }

        private async void FontSizeCombo_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            CodeControl.FontSize = (double) FontSizeCombo.SelectedItem;
            // await CodeControl.UpdateTextSourceAsync();
        }

        private void OnExecutedHideToolBar(object sender, ExecutedRoutedEventArgs e)
        {
        }

        private void OpenFile(object sender, RoutedEventArgs e)
        {
            var d = new OpenFileDialog {Filter = "CSharp Source|*.cs"};
            if (!d.ShowDialog().GetValueOrDefault()) return;
            Filename = d.FileName;
            // CodeControl.Filename = d.FileName;
            JTF.RunAsync(LoadFileOrProjectAsync);
        }

        private void PrintFile(object sender, RoutedEventArgs e)
        {
            var d = new PrintDialog();
            if (!d.ShowDialog().GetValueOrDefault())
                return;
            d.PrintDocument(CodeControl.CodeControl.DocumentPaginator, "code file");
        }

        private void OnPrintExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            var pd = new PrintDialog();
            var r = pd.ShowDialog();
            if (!r.GetValueOrDefault())
                return;
            pd.PrintDocument(CodeControl.CodeControl.DocumentPaginator, CodeControl.CodeControl.DocumentTitle);
        }

        public static readonly DependencyProperty DocumentProperty = DependencyProperty.Register(
            "Document", typeof(Document), typeof(MainWindow),
            new PropertyMetadata(default(Document), OnDocumentChanged));

        // ReSharper disable once MemberCanBePrivate.Global
        public Document Document
        {
            get { return (Document) GetValue(DocumentProperty); }
            set { SetValue(DocumentProperty, value); }
        }

        private static void OnDocumentChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((MainWindow) d).OnDocumentChanged((Document) e.OldValue, (Document) e.NewValue);
        }

        public static readonly DependencyProperty ProjectProperty = DependencyProperty.Register(
            "Project", typeof(Project), typeof(MainWindow), new PropertyMetadata(default(Project), OnProjectChanged));

        private Task _task;
        private MefHostServices _host;
        public static readonly DependencyProperty DiagnosticsProperty = DependencyProperty.Register("Diagnostics", typeof(IEnumerable), typeof(MainWindow), new PropertyMetadata(default(IEnumerable)));

        public Project Project
        {
            get { return (Project) GetValue(ProjectProperty); }
            set { SetValue(ProjectProperty, value); }
        }

        public IEnumerable Diagnostics
        {
            get { return (IEnumerable) GetValue(DiagnosticsProperty); }
            set { SetValue(DiagnosticsProperty, value); }
        }

        private static void OnProjectChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((MainWindow) d).OnProjectChanged((Project) e.OldValue, (Project) e.NewValue);
        }


        protected virtual void OnProjectChanged(Project oldValue, Project newValue)
        {
            if (newValue != null) Ellipse.Fill = Brushes.LawnGreen;
        }

        protected virtual void OnDocumentChanged(Document oldValue, Document newValue)
        {
        }

        private async Task LoadFileOrProjectAsync()
        {
            if (Filename != null && Filename.EndsWith(".csproj"))
            {
                await LoadProjectAsync(Filename);
                return;
            }

            var w = _workspace = new AdhocWorkspace(_host);
            w.AddSolution(SolutionInfo.Create(SolutionId.CreateNewId(), VersionStamp.Create()));
            var projectInfo = ProjectInfo.Create(ProjectId.CreateNewId(), VersionStamp.Create(),
                "Code Project", "code", LanguageNames.CSharp);
            var w2 = w.CurrentSolution.AddProject(projectInfo);
            w.TryApplyChanges(w2);

            DocumentInfo documentInfo = null;
            if (Filename != null)
                documentInfo = DocumentInfo.Create(DocumentId.CreateNewId(projectInfo.Id), "Default",
                    null, SourceCodeKind.Regular, new FileTextLoader(Filename, Encoding.UTF8), Filename);
            else
                documentInfo = DocumentInfo.Create(DocumentId.CreateNewId(projectInfo.Id), "Default",
                    null, SourceCodeKind.Regular);

            w2 = w.CurrentSolution.AddDocument(documentInfo);
            w.TryApplyChanges(w2);

            Project = w.CurrentSolution.GetProject(projectInfo.Id);
            Document = w.CurrentSolution.GetDocument(documentInfo.Id);
        }

      
        private async Task LoadProjectAsync(string s)
        {
            // StatusScrollViewer.Visibility = Visibility.Visible;
            // status.Visibility = Visibility.Visible;

            var ww = MSBuildWorkspace.Create();
            var project = await ww.OpenProjectAsync(s, new Progress1(this)).ConfigureAwait(true);
            Project = project;

            foreach (var d in project.Documents)
            {
                var tree = await d.GetSyntaxTreeAsync();
                var model = await d.GetSemanticModelAsync();
                foreach (var (item1, item2, item3) in AnalyzerContexts.SelectMany(z => z.SyntaxNodeActions))
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



                                        status.Text += message + "\r\n\r\n";

                                        StatusScrollViewer.ScrollToBottom();

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
            StatusScrollViewer.Visibility = Visibility.Hidden;
            status.Visibility = Visibility.Hidden;

        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void OnDocumentOpen(object sender, ExecutedRoutedEventArgs e)
        {
            if (e.Parameter is Document d)
            {
                Document = d;
                JTF.RunAsync(SetupCodeControlAsync);
            }
        }

        private void PowershellExecuted(object sender, RoutedEventArgs e)
        {
#if POWERSHELL

            var shell = new WpfTerminalControl();
            shell.BeginInit();
            shell.ForegroundColor = ConsoleColor.Black;
            shell.BackgroundColor = ConsoleColor.White;
            shell.Background = Brushes.White;
            ;
            shell.Foreground = Brushes.Black;
            ;
            shell.AutoResize = true;
            shell.CursorBrush = Brushes.Orange;
            shell.EndInit();
            var wrapped = new WrappedPowerShell();
            wrapped.BeginInit();
            wrapped.Terminal = shell;
            wrapped.EndInit();
            wrapped.CoerceValue(WrappedPowerShell.InitialSessionStateProperty);
            wrapped.CoerceValue(WrappedPowerShell.RunspaceProperty);

            shell.TextEntryComplete += (o, args) => JTF.RunAsync(async () =>
            {
                await wrapped.ExecuteAsync(args.Text);
            });

            Tabs.Items.Add(new TabItem() {Header = "PowerShell", Content = shell});
            Tabs.SelectedIndex = Tabs.Items.Count - 1;

#endif
        }

        private void SearchNuget(object sender, RoutedEventArgs e)
        {
            var w = new Window();
            var nugetSearch = new NugetSearch {AnalyzersDir = AnalyzersDir};
            w.Content = nugetSearch;
            w.ShowDialog();
            AddAnalyzerDlls(nugetSearch.SavedFiles);
        }

        private void AddAnalyzerDlls(List<string> files)
        {
            foreach (var file in files)
            {
                LoadAnalyzers(file);
            }
        }
    }

    public class AppViewModel
    {

    }
    }