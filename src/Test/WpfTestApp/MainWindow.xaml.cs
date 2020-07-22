﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
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
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.VisualStudio.Threading;
using Microsoft.Win32;
using RoslynCodeControls;

namespace WpfTestApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public static RoutedUICommand HideToolBar =
            new RoutedUICommand("Hide toolbar", nameof(HideToolBar), typeof(MainWindow));

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
            InitializeComponent();
            CoerceValue(FontsProperty);
            Loaded += OnLoaded;


            CommandBindings.Add(new CommandBinding(HideToolBar, OnExecutedHideToolBar));
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            JTF.RunAsync(M1);
        }

        private async Task M1()
        {
            Thread.CurrentThread.Name = "App thread";
            _host = MefHostServices.Create(MefHostServices.DefaultAssemblies);

            await LoadFileOrProject();
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
            CodeControl.JTF2 = JTF2;
            CodeControl.Document = Document;
            CodeControl.SyntaxTree = await Document.GetSyntaxTreeAsync();
            var semanticModelAsync = await Document.GetSemanticModelAsync();
            CodeControl.SemanticModel = semanticModelAsync;
            if (semanticModelAsync != null) CodeControl.Compilation = semanticModelAsync.Compilation;


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
        }

        public DateTime StartTime { get; set; }

        public JoinableTaskFactory JTF { get; set; } = new JoinableTaskFactory(new JoinableTaskContext());
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
            JTF.RunAsync(LoadFileOrProject);
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
        
        public Project Project
        {
            get { return (Project) GetValue(ProjectProperty); }
            set { SetValue(ProjectProperty, value); }
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

        public async Task LoadFileOrProject()
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

        // private void Target1(object sender, RoutedEventArgs e)
        // {
            // Ellipse2.Fill = Brushes.Orange;
        // }

        // private void Target(object sender, RoutedEventArgs e)
        // {
            // Ellipse2.Fill = Brushes.GreenYellow;
        // }

        private async Task LoadProjectAsync(string s)
        {
            StatusScrollViewer.Visibility = Visibility.Visible;
            status.Visibility = Visibility.Visible;

            var ww = MSBuildWorkspace.Create();
            var project = await ww.OpenProjectAsync(s, new Progress1(this)).ConfigureAwait(true);
            Project = project;
            StatusScrollViewer.Visibility = Visibility.Hidden;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        // private void Combo_OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        // {
        // combo.SelectedIndex = 0;
        // }
        private void OnDocumentOpen(object sender, ExecutedRoutedEventArgs e)
        {
            if (e.Parameter is Document d)
            {
                Document = d;
                JTF.RunAsync(SetupCodeControlAsync);
            }
        }
    }
}