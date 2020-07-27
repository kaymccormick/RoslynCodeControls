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

namespace ClassDiagram
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        public string Filename { get; set; }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            JTF.RunAsync(OnLoadedAsync);
        }

        private JoinableTaskFactory JTF { get; } = new JoinableTaskFactory(new JoinableTaskContext());

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
            CodeControl.XOffset = 100;
            CodeControl.JTF2 = JTF2;
            CodeControl.PreviewKeyDown += CodeControlOnPreviewKeyDown;
            var doc = w.CurrentSolution.GetDocument(documentInfo.Id);
            if (doc != null)
            {
                ClassDiagram.Document = doc;
                CodeControl.Document = doc;
                var tree = await doc.GetSyntaxTreeAsync();
                // ReSharper disable once AssignNullToNotNullAttribute
                CodeControl.SyntaxTree = tree;
                var model = await doc.GetSemanticModelAsync();
                // ReSharper disable once AssignNullToNotNullAttribute
                CodeControl.SemanticModel = model;
                CodeControl.Compilation = model.Compilation;
            }

            DateTime startTime = default;
            CodeControl.AddHandler(RoslynCodeBase.RenderStartEvent, new RoutedEventHandler((sender, args) =>
            {
                startTime = DateTime.Now;
                Debug.WriteLine("render start");
            }));
            CodeControl.AddHandler(RoslynCodeBase.RenderCompleteEvent, new RoutedEventHandler((sender, args) =>
            {
                var span = DateTime.Now - startTime;
                var msg = $"render complete time is {span}";
                Debug.WriteLine(msg);
                ProtoLogger.Instance.LogAction(msg);
            }));
            await CodeControl.UpdateFormattedTextAsync();
            CodeControlRendered = true;
            Rendering.Visibility = Visibility.Collapsed;
        }

        public bool CodeControlRendered { get; set; }

        /// <inheritdoc />
        protected override void OnPreviewKeyDown(KeyEventArgs e)
        {
            base.OnPreviewKeyDown(e);
            if (e.Key != Key.Tab || Keyboard.Modifiers != ModifierKeys.Control) return;
            CodeControl.Visibility = CodeControl.Visibility == Visibility.Visible ? Visibility.Hidden : Visibility.Visible;
        }

        private void CodeControlOnPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Escape) return;
            e.Handled = true;
            CodeControl.Visibility = Visibility.Hidden;
        }

        public JoinableTaskFactory JTF2 { get; set; }

        public MefHostServices Host { get; set; }

        private void Find(object sender, ExecutedRoutedEventArgs e)
        {
            if (!(e.Parameter is EntityMember cem)) return;
            CodeControl.Visibility = Visibility.Visible;
            Panel.SetZIndex(CodeControl, 1);
            double? y = null;
            var sourceSpanStart = cem.Location.SourceSpan.Start;
            foreach (var lineInfo2 in CodeControl.LineInfos2)
            {
                var lineInfo2Offset = lineInfo2.Offset + lineInfo2.Length;

                if (lineInfo2Offset < sourceSpanStart)
                    continue;
                y = lineInfo2.Origin.Y;
                break;
            }

            if (y != null)
                if (CodeControl.ScrollViewer != null)
                    CodeControl.ScrollViewer.ScrollToVerticalOffset(y.Value);
                else
                    CodeControl.InitialScrollPosition = y.Value;
        }
    }
}