using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
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
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.QuickInfo;
using Microsoft.VisualStudio.Threading;
using RoslynCodeControls;

namespace BasicEditor
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public JoinableTaskFactory JTF2 { get; set; }

        public MainWindow()
        {
            InitializeComponent();
            CodeControl.Focus();
            Keyboard.Focus(CodeControl);

            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            JTF.RunAsync(OnLoadedAsync);
        }

        private async Task OnLoadedAsync()
        {
            Thread.CurrentThread.Name = "App thread";
            var a = new[]
            {
                "Microsoft.CodeAnalysis.Workspaces",
                "Microsoft.CodeAnalysis.CSharp.Workspaces",
                // "Microsoft.CodeAnalysis.VisualBasic.Workspaces",
                "Microsoft.CodeAnalysis.Features",
                "Microsoft.CodeAnalysis.CSharp.Features",
                // "Microsoft.CodeAnalysis.Workspaces.MSBuild",
                "Microsoft.CodeAnalysis.Metrics"
                //, "Microsoft.CodeAnalysis.VisualBasic.Features"
            };

            // LoadAssemblies();
            // foreach (var enumerateFile in Directory.EnumerateFiles(System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "*.dll"))
            // {
                // Assembly.LoadFrom(enumerateFile);
            // }
            // foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            // {
                // Debug.WriteLine(assembly.GetName().Name);
            // }
            var mefa= AppDomain.CurrentDomain.GetAssemblies().Where(assembly => a.Contains(assembly.GetName().Name)).ToList();
            foreach (var s in mefa.Select(z=>z.GetName().Name))
            {
                Debug.WriteLine(s);
            }
            Host = MefHostServices.Create(MefHostServices.DefaultAssemblies);
            
            await SetupCodeControlAsync();

        }

        private static void LoadAssemblies()
        {
            var loaded = AppDomain.CurrentDomain.GetAssemblies().Select(x => x.FullName).ToHashSet();

            void RecursiveLoad(AssemblyName name)
            {
                if (loaded.Contains(name.FullName))
                    return;
                loaded.Add(name.FullName);
                Assembly a;
                try
                {
                    Debug.WriteLine("Loading " + name);
                    a = Assembly.Load(name);
                }
                catch
                {
                    return;
                }

                foreach (var assemblyName in a.GetReferencedAssemblies().Where(x => !loaded.Contains(x.FullName)))
                    RecursiveLoad(assemblyName);
            }

            foreach (var name in Assembly.GetExecutingAssembly().GetReferencedAssemblies()) RecursiveLoad(name);
        }

        private async Task SetupCodeControlAsync()
        {
            var w = new AdhocWorkspace(Host);
#if false
            foreach (var fieldInfo in w.GetType().GetFields(BindingFlags.FlattenHierarchy|BindingFlags.Instance|BindingFlags.NonPublic))
            {
                var v = fieldInfo.GetValue(w);
                Debug.WriteLine($"{fieldInfo.Name}: {v}");
            }

            var langSvc = w.Services.GetLanguageServices(LanguageNames.CSharp);
            var method = langSvc.GetType().GetMethod("GetService", BindingFlags.Instance | BindingFlags.Public);

            List<object> services = new List<object>();
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    foreach (var type in assembly.GetTypes())
                    {
                        if (typeof(ILanguageService).IsAssignableFrom(type))
                        {
                            try
                            {
                                if (!type.ContainsGenericParameters)
                                {
                                    var m = method.MakeGenericMethod(new[] {type});
                                    var result = m.Invoke(langSvc, new object[] { });
                                    if (result != null)
                                    {
                                        Debug.WriteLine(result);
                                        services.Add(result);
                                    }
                                }
                            }
                            catch
                            {

                            }

                            if (type.IsPublic)
                                Debug.WriteLine(String.Format("{0:G5}", type.IsPublic.ToString()) + " " +
                                                type.FullName);
                        }
                    }
                }
                catch
                {

                }
            }
            foreach (object service in services)
            {
                foreach (var methodInfo in service.GetType().GetMethods(BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic))
                {
                    Debug.WriteLine((methodInfo.ReturnType?.FullName ?? "") + " " + service.GetType().FullName + "." + methodInfo.Name + "( " +
                                    string.Join(", ", methodInfo.GetParameters().Select(p => (p.IsOptional ? " optional " : "") + p.ParameterType + " " + p.Name)) + ")");

                }

                if (service.GetType().Name == "CSharpCodeCleanupService")
                {

                }
            }
            
            // w.Services.FindLanguageServices<IFormattingService>(metadata =>
            // {
                // foreach (var (key, value) in metadata)
                // {

                    // Debug.WriteLine($"{key} = {value}");
                // }

                // Debug.WriteLine("");

                // return false;
            // });
#endif      
            w.AddSolution(SolutionInfo.Create(SolutionId.CreateNewId(), VersionStamp.Create()));
            var projectInfo = ProjectInfo.Create(ProjectId.CreateNewId(), VersionStamp.Create(),
                "Code Project", "code", LanguageNames.CSharp);
            var w2 = w.CurrentSolution.AddProject(projectInfo);
            w.TryApplyChanges(w2);

            DocumentInfo documentInfo;
            var filename = Filename;
            if (filename != null)
                documentInfo = DocumentInfo.Create(DocumentId.CreateNewId(projectInfo.Id), "Default",
                    null, SourceCodeKind.Regular, new FileTextLoader(filename, Encoding.UTF8), filename);
            else
                documentInfo = DocumentInfo.Create(DocumentId.CreateNewId(projectInfo.Id), "Default",
                    null, SourceCodeKind.Regular);

            w2 = w.CurrentSolution.AddDocument(documentInfo);
            w.TryApplyChanges(w2);

            Project1 = w.CurrentSolution.GetProject(projectInfo.Id);
            Document1 = w.CurrentSolution.GetDocument(documentInfo.Id);
            

            var qui = w.Services.GetLanguageServices(LanguageNames.CSharp).GetService<QuickInfoService>();
            var textAsync = await Document1.GetTextAsync();
            if(false)
            {
                for (var i = 0; i < textAsync.Length; i++)
                {
                    var re = await qui.GetQuickInfoAsync(Document1, i);
                    if (re != null)
                    {
                        Debug.WriteLine(re.Span.ToString());
                        Debug.WriteLine("tags = "+ string.Join(";", re.Tags));
                        foreach (var reRelatedSpan in re.RelatedSpans)
                        {
                            Debug.WriteLine("relatedspan " + reRelatedSpan.ToString());
                        }

                        foreach (var quickInfoSection in re.Sections)
                        {
                            Debug.WriteLine("" + i + " Text(" + quickInfoSection.Text + ") Kind(" + quickInfoSection.Kind + ") TaggedParts(" +
                                            String.Join(", ", quickInfoSection.TaggedParts)+")");

                        }
                    }
                }
            }

            CodeControl.DebugLevel = 2;
            CodeControl.JTF2 = JTF2;
            CodeControl.Document = Document1;
            var tree = await Document1.GetSyntaxTreeAsync();
            CodeControl.SyntaxTree = tree;
            var model = await Document1.GetSemanticModelAsync();
            CodeControl.SemanticModel = model;
            CodeControl.Compilation = model.Compilation;

            // CodeControl.AddHandler(RoslynCodeControl.ContentChangedEvent, new RoslynCodeControl.ContentChangedRoutedEventHandler(CodeControlContentChanged));
            CodeControl.AddHandler(RoslynCodeBase.RenderStartEvent, new RoutedEventHandler((sender, args) =>
            {
                // StartTime = DateTime.Now;
                Debug.WriteLine("render start");
            }));
            CodeControl.AddHandler(RoslynCodeBase.RenderCompleteEvent, new RoutedEventHandler((sender, args) =>
            {
                // var span = DateTime.Now - StartTime;
                Debug.WriteLine("render complete " );
            }));

            await CodeControl.UpdateFormattedTextAsync();

        }

        public Document Document1 { get; set; }

        public Project Project1 { get; set; }


        private JoinableTaskFactory JTF { get; set; } = new JoinableTaskFactory(new JoinableTaskContext());


        public MefHostServices Host { get; set; }

        public string Filename { get; set; }

        private void Paste(object sender, ExecutedRoutedEventArgs e)
        {
            JTF.RunAsync(DoPasteAsync);
        }

        private async Task DoPasteAsync()
        {
            if (Clipboard.ContainsText())
            {
                var t = Clipboard.GetText();
                Debug.WriteLine(t);
                if (CodeControl is RoslynCodeControl c)
                {
                    await c.DoInputAsync(new InputRequest(InputRequestKind.TextInput, t));
                }
                // foreach (var s in t.Split("\r\n"))

                // {
                    // foreach (var ch in s)
                    // {
                        // await CodeControl.DoInputAsync(new InputRequest(InputRequestKind.TextInput, ch.ToString()));
                    // }

                    // await CodeControl.DoInputAsync(new InputRequest(InputRequestKind.NewLine));
                // }
            }
        }

        private void Print(object sender, ExecutedRoutedEventArgs e)
        {
            var d = new PrintDialog();
            if (!d.ShowDialog().GetValueOrDefault())
                return;
            d.PrintDocument(CodeControl.DocumentPaginator, "code file");

        }
    }
}
