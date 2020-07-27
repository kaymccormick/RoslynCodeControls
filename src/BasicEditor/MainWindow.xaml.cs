using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
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
using Microsoft.CodeAnalysis.Host.Mef;
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
            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            JTF.RunAsync(OnLoadedAsync);
        }

        private async Task OnLoadedAsync()
        {
            Thread.CurrentThread.Name = "App thread";
            Host = MefHostServices.Create(MefHostServices.DefaultAssemblies);

            await SetupCodeControlAsync();

        }

        private async Task SetupCodeControlAsync()
        {
            var w = new AdhocWorkspace(Host);
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
                await CodeControl.DoInputAsync(new InputRequest(InputRequestKind.TextInput, t));
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
