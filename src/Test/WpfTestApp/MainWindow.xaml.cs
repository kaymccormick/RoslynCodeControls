using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Markup.Localizer;
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

        public static IEnumerable<double> CommonFontSizes
        {
            get
            {
                return new[]
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

        public AdhocWorkspace _workspace;

        public ICommand DefaultHideToolBarCommand
        {
            get { return (ICommand) GetValue(DefaultHideToolBarCommandProperty); }
            set { SetValue(DefaultHideToolBarCommandProperty, value); }
        }

        public JoinableTaskFactory JTF2 { get; set; }

        public MainWindow()
        {
            var a1 = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => a.DefinedTypes.Where(t => t.Name == "DefaultAnalyzerAssemblyLoader"));
            if (a1.Any())
            {
                foreach (var typeInfo in a1)
                {
                    Debug.WriteLine(typeInfo.ToString());
                    Debug.WriteLine(typeInfo.Assembly.ToString());
                    var loader = Activator.CreateInstance(typeInfo);
                    AnalyzerLoader = (IAnalyzerAssemblyLoader) loader;
                    // foreach (var constructorInfo in typeInfo.GetConstructors())
                    // {
                    //     Debug.WriteLine(constructorInfo.GetParameters().Length);
                    // }
                    break;
                }
            }
            ViewModel = new AppViewModel();
            Diagnostics = _diagnosticsList;
            var tmp = Path.GetTempFileName();
            File.Delete(tmp);
            Directory.CreateDirectory(tmp);
            WorkDir = tmp;
            AnalyzersDir = Path.Combine(WorkDir, "analyzers");
            Directory.CreateDirectory(AnalyzersDir);
            Debug.WriteLine($"Analyzers dir is {AnalyzersDir}");
            InitializeComponent();
            CoerceValue(FontsProperty);
            Loaded += OnLoaded;
            // LoadAnalyzers(@"C:\temp\roslyn.analyzers.dll");

            CommandBindings.Add(new CommandBinding(HideToolBar, OnExecutedHideToolBar));
        }

        public IAnalyzerAssemblyLoader AnalyzerLoader { get; set; }

        private string AnalyzersDir { get; set; }

        private string WorkDir { get; set; }

        public void LoadAnalyzers(ICollection<MyAnalyzerContext> myAnalyzerContexts, string dllFile)
        {
            Project = Project.WithAnalyzerReferences(Project.AnalyzerReferences.Concat(new[]
                {new AnalyzerFileReference(dllFile, AnalyzerLoader)}));
            return;
            var analyzersAssembly  = Assembly.LoadFrom(dllFile);
            var analyzerTypes = analyzersAssembly.ExportedTypes.Where(t => typeof(DiagnosticAnalyzer).IsAssignableFrom(t) &&  t.GetCustomAttribute<DiagnosticAnalyzerAttribute>() != null).ToList();
            foreach (var analyzerType in analyzerTypes)
            {
                // Debug.WriteLine(analyzerType);
                var  z= (DiagnosticAnalyzer)Activator.CreateInstance(analyzerType);
                if (z == null) continue;
                var c = new MyAnalyzerContext(z);
                z.Initialize(c);
                myAnalyzerContexts.Add(c);
            }
        }

        public List<MyAnalyzerContext> AnalyzerContexts { get; set; } = new List<MyAnalyzerContext>();

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            JTF.RunAsync(OnLoadedAsync);
        }

        private async Task OnLoadedAsync()
        {
            Thread.CurrentThread.Name = "App thread";
            _host = MefHostServices.Create(MefHostServices.DefaultAssemblies);

            await ViewModel.LoadFileOrProjectAsync(this, this.Filename);
            await SetupCodeControlAsync();

            StartupCommand?.Execute(null);
        }

        private async Task SetupCodeControlAsync()
        {
            CodeControl.DebugLevel = 2;
            CodeControl.JTF2 = JTF2;
            CodeControl.Document = Document;
            var tree = await Document.GetSyntaxTreeAsync();
            CodeControl.SyntaxTree = tree;
            var model = await Document.GetSemanticModelAsync();
            CodeControl.SemanticModel = model;
            if (model != null) CodeControl.Compilation = model.Compilation;

            CodeControl.AddHandler(RoslynCodeControl.ContentChangedEvent, new RoslynCodeControl.ContentChangedRoutedEventHandler(CodeControlContentChanged));
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

            var d = ViewModel.RunAnalyzers(CodeControl, AnalyzerContexts, tree, model);
            _diagnosticsList.Clear();
            foreach (var diagnostic in d)
            {
                _diagnosticsList.Add(diagnostic);
                
            }
        }

        private void CodeControlContentChanged(object sender, ContentChangedRoutedEventArgs e)
        {
            var c = (RoslynCodeControl)e.OriginalSource;
            var tree = c.SyntaxTree;
            var model = c.SemanticModel;
            var d = ViewModel.RunAnalyzers(CodeControl, AnalyzerContexts, tree, model);
            _diagnosticsList.Clear();
            foreach (var diagnostic in d)
            {
                _diagnosticsList.Add(diagnostic);

            }
        }

        private DateTime StartTime { get; set; }

        private JoinableTaskFactory JTF { get; set; } = new JoinableTaskFactory(new JoinableTaskContext());
        public string Filename { get; set; }
        public ICommand StartupCommand { get; set; }

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
            JTF.RunAsync(() => ViewModel.LoadFileOrProjectAsync(this, this.Filename));
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
        public MefHostServices _host;
        public static readonly DependencyProperty DiagnosticsProperty = DependencyProperty.Register("Diagnostics", typeof(IEnumerable), typeof(MainWindow), new PropertyMetadata(default(IEnumerable)));
        private readonly ObservableCollection<Diagnostic> _diagnosticsList = new ObservableCollection<Diagnostic>();

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


        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void OnOpen(object sender, ExecutedRoutedEventArgs e)
        {
            switch (e.Parameter)
            {
                case Document d:
                    Document = d;
                    JTF.RunAsync(SetupCodeControlAsync);
                    return;
                case Project p:
                    Project = p;
                    return;
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

        private void AddAnalyzerDlls(IEnumerable<string> files)
        {
            foreach (var file in files)
            {
	    AnalyzerDlls.Add(file);
                LoadAnalyzers(AnalyzerContexts, file);
            }
        }

        public List<string> AnalyzerDlls  { get; set; } = new List<string>();

        private void Compile(object sender, RoutedEventArgs e)
        {
            JTF.RunAsync(() => ViewModel.CompileAsync(Project, this));
        }
    }
    internal class DefaultAnalyzerAssemblyLoader : AnalyzerAssemblyLoader
    {
        private AssemblyLoadContext _loadContext;

        protected override Assembly LoadFromPathImpl(string fullPath)
        {
            //.NET Native doesn't support AssemblyLoadContext.GetLoadContext. 
            // Initializing the _loadContext in the .ctor would cause
            // .NET Native builds to fail because the .ctor is called. 
            // However, LoadFromPathImpl is never called in .NET Native, so 
            // we do a lazy initialization here to make .NET Native builds happy.
            if (_loadContext == null)
            {
                AssemblyLoadContext loadContext = AssemblyLoadContext.GetLoadContext(typeof(DefaultAnalyzerAssemblyLoader).GetTypeInfo().Assembly);

                if (System.Threading.Interlocked.CompareExchange(ref _loadContext, loadContext, null) == null)
                {
                    _loadContext.Resolving += (context, name) =>
                    {
                        Debug.Assert(ReferenceEquals(context, _loadContext));
                        return Load(name.FullName);
                    };
                }
            }

            return LoadImpl(fullPath);
        }

        protected virtual Assembly LoadImpl(string fullPath) => _loadContext.LoadFromAssemblyPath(fullPath);
    }
    internal abstract class AnalyzerAssemblyLoader : IAnalyzerAssemblyLoader
    {
        private readonly object _guard = new object();

        // lock _guard to read/write
        private readonly Dictionary<string, Assembly> _loadedAssembliesByPath = new Dictionary<string, Assembly>();
        private readonly Dictionary<string, AssemblyIdentity> _loadedAssemblyIdentitiesByPath = new Dictionary<string, AssemblyIdentity>();
        private readonly Dictionary<AssemblyIdentity, Assembly> _loadedAssembliesByIdentity = new Dictionary<AssemblyIdentity, Assembly>();

        // maps file name to a full path (lock _guard to read/write):
        private readonly Dictionary<string, HashSet<string>> _knownAssemblyPathsBySimpleName = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);

        protected abstract Assembly LoadFromPathImpl(string fullPath);

        #region Public API

        public void AddDependencyLocation(string fullPath)
        {
            // CompilerPathUtilities.RequireAbsolutePath(fullPath, nameof(fullPath));
            string simpleName = Path.GetFileNameWithoutExtension(fullPath);

            lock (_guard)
            {
                if (!_knownAssemblyPathsBySimpleName.TryGetValue(simpleName, out var paths))
                {
                    paths = new HashSet<string>();
                    _knownAssemblyPathsBySimpleName.Add(simpleName, paths);
                }

                paths.Add(fullPath);
            }
        }

        public Assembly LoadFromPath(string fullPath)
        {
            // CompilerPathUtilities.RequireAbsolutePath(fullPath, nameof(fullPath));
            return LoadFromPathUnchecked(fullPath);
        }

        #endregion

        private Assembly LoadFromPathUnchecked(string fullPath)
        {
            return LoadFromPathUncheckedCore(fullPath);
        }

        private Assembly LoadFromPathUncheckedCore(string fullPath, AssemblyIdentity identity = null)
        {
            // Debug.Assert(PathUtilities.IsAbsolute(fullPath));

            // Check if we have already loaded an assembly with the same identity or from the given path.
            Assembly loadedAssembly = null;
            lock (_guard)
            {
                if (_loadedAssembliesByPath.TryGetValue(fullPath, out var existingAssembly))
                {
                    loadedAssembly = existingAssembly;
                }
                else
                {
                    identity ??= GetOrAddAssemblyIdentity(fullPath);
                    if (identity != null && _loadedAssembliesByIdentity.TryGetValue(identity, out existingAssembly))
                    {
                        loadedAssembly = existingAssembly;
                    }
                }
            }

            // Otherwise, load the assembly.
            if (loadedAssembly == null)
            {
                loadedAssembly = LoadFromPathImpl(fullPath);
            }

            // Add the loaded assembly to both path and identity cache.
            return AddToCache(loadedAssembly, fullPath, identity);
        }

        private Assembly AddToCache(Assembly assembly, string fullPath, AssemblyIdentity identity)
        {
            // Debug.Assert(PathUtilities.IsAbsolute(fullPath));
            Debug.Assert(assembly != null);

            identity = AddToCache(fullPath, identity ?? AssemblyIdentity.FromAssemblyDefinition(assembly));
            Debug.Assert(identity != null);

            lock (_guard)
            {
                // The same assembly may be loaded from two different full paths (e.g. when loaded from GAC, etc.),
                // or another thread might have loaded the assembly after we checked above.
                if (_loadedAssembliesByIdentity.TryGetValue(identity, out var existingAssembly))
                {
                    assembly = existingAssembly;
                }
                else
                {
                    _loadedAssembliesByIdentity.Add(identity, assembly);
                }

                // An assembly file might be replaced by another file with a different identity.
                // Last one wins.
                _loadedAssembliesByPath[fullPath] = assembly;

                return assembly;
            }
        }

        private AssemblyIdentity GetOrAddAssemblyIdentity(string fullPath)
        {
            // Debug.Assert(PathUtilities.IsAbsolute(fullPath));

            lock (_guard)
            {
                if (_loadedAssemblyIdentitiesByPath.TryGetValue(fullPath, out var existingIdentity))
                {
                    return existingIdentity;
                }
            }

            // var identity = null;//AssemblyIdentityUtils.TryGetAssemblyIdentity(fullPath);
            return AddToCache(fullPath, null);
        }

        private AssemblyIdentity AddToCache(string fullPath, AssemblyIdentity identity)
        {
            lock (_guard)
            {
                if (_loadedAssemblyIdentitiesByPath.TryGetValue(fullPath, out var existingIdentity) && existingIdentity != null)
                {
                    identity = existingIdentity;
                }
                else
                {
                    _loadedAssemblyIdentitiesByPath[fullPath] = identity;
                }
            }

            return identity;
        }

        public Assembly Load(string displayName)
        {
            if (!AssemblyIdentity.TryParseDisplayName(displayName, out var requestedIdentity))
            {
                return null;
            }

            ImmutableArray<string> candidatePaths;
            lock (_guard)
            {

                // First, check if this loader already loaded the requested assembly:
                if (_loadedAssembliesByIdentity.TryGetValue(requestedIdentity, out var existingAssembly))
                {
                    return existingAssembly;
                }
                // Second, check if an assembly file of the same simple name was registered with the loader:
                if (!_knownAssemblyPathsBySimpleName.TryGetValue(requestedIdentity.Name, out var pathList))
                {
                    return null;
                }

                Debug.Assert(pathList.Count > 0);
                candidatePaths = pathList.ToImmutableArray();
            }

            // Multiple assemblies of the same simple name but different identities might have been registered.
            // Load the one that matches the requested identity (if any).
            foreach (var candidatePath in candidatePaths)
            {
                var candidateIdentity = GetOrAddAssemblyIdentity(candidatePath);

                if (requestedIdentity.Equals(candidateIdentity))
                {
                    return LoadFromPathUncheckedCore(candidatePath, candidateIdentity);
                }
            }

            return null;
        }
    }

}