using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.VisualStudio.Threading;

namespace RoslynCodeControls
{
    public class RoslynCodeBase : Control, ICodeView, IDocumentPaginatorSource
    {
        /// <inheritdoc />
        public RoslynCodeBase()
        {
            TextDestination = new DrawingGroup();
            DrawingBrush = new DrawingBrush();
            UpdateChannel = Channel.CreateUnbounded<UpdateInfo>(new UnboundedChannelOptions()
                {SingleReader = true, SingleWriter = true});
            _reader = UpdateChannel.Reader;
            
        }

        static RoslynCodeBase()
        {
            // TextElement.FontSizeProperty.AddOwner(typeof(RoslynCodeBase));
            // TextElement.FontFamilyProperty.AddOwner(typeof(RoslynCodeBase));

            RoslynProperties.SourceTextProperty.AddOwner(typeof(RoslynCodeBase),
                new PropertyMetadata(default(string), OnSourceTextUpdated));
            RoslynProperties.SemanticModelProperty.AddOwner(typeof(RoslynCodeBase));
            RoslynProperties.CompilationProperty.AddOwner(typeof(RoslynCodeBase));
            RoslynProperties.DocumentProperty.AddOwner(typeof(RoslynCodeBase));
            RoslynProperties.SyntaxNodeProperty.AddOwner(typeof(RoslynCodeBase));
            RoslynProperties.SyntaxTreeProperty.AddOwner(typeof(RoslynCodeBase));
        }

        private static void OnSourceTextUpdated(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var f = (RoslynCodeBase) d;

            f.OnSourceTextChanged1((string) e.NewValue, (string) e.OldValue);
        }

        
        public static readonly DependencyProperty CompilationProperty = RoslynProperties.CompilationProperty;

        public Compilation Compilation
        {
            get { return (Compilation) GetValue(CompilationProperty); }
            set { SetValue(CompilationProperty, value); }
        }

        protected virtual async void OnSourceTextChanged1(string newValue, string eOldValue)
        {
            if (ChangingText || UpdatingSourceText)
                return;
            if (newValue != null)
            {
                UpdatingSourceText = true;
                var compilation = CSharpCompilation.Create(
                    "test",
                    new[]
                    {
                        SyntaxFactory.ParseSyntaxTree(
                            newValue)
                    }, new[] {MetadataReference.CreateFromFile(typeof(object).Assembly.Location)});
                Compilation = compilation;
                SyntaxTree = compilation.SyntaxTrees.First();

                SyntaxNode = await SyntaxTree.GetRootAsync().ConfigureAwait(true);
                SemanticModel = compilation.GetSemanticModel(SyntaxTree);
                UpdatingSourceText = false;
            }
        }

        public bool UpdatingSourceText { get; set; }

        public bool ChangingText { get; set; }

        private void ContinuationFunction(Task<UpdateInfo> z)
        {
            var ui = z.Result;
            // fixme
            //CharInfos.AddRange(ui.CharInfos);
            var dg = ui.DrawingGroup;
            var dg2 = new DrawingGroup();
            foreach (var dgChild in dg.Children)
                dg2.Children.Add(dgChild);
            TextDestination.Children.Add(dg2);
            var uiRect = dg2.Bounds;
            Debug.WriteLine($"UIRect is {uiRect}");
            var maxY = Math.Max(MaxY, uiRect.Bottom);
            MaxY = maxY;
            var maxX = Math.Max(MaxX, uiRect.Right);
            MaxX = maxX;
            // bound to viewbox height / width
            // Rectangle.Height = maxY;
            // Rectangle.Width = maxX;
            var boundsLeft = Math.Min(TextDestination.Bounds.Left, 0);
            boundsLeft -= 3;
            var boundsTop = Math.Min(TextDestination.Bounds.Top, 0);
            boundsTop -= 3;
            DrawingBrushViewbox = new Rect(boundsLeft, boundsTop, maxX, maxY);
          
        }

        public virtual Rect DrawingBrushViewbox { get; set; }

        public double MaxX { get; set; }

        public double MaxY { get; set; }

        /// <inheritdoc />
        public virtual DrawingBrush DrawingBrush { get; }

        public static readonly DependencyProperty DocumentProperty = RoslynProperties.DocumentProperty;

        public Document Document
        {
            get { return (Document) GetValue(DocumentProperty); }
            set { SetValue(DocumentProperty, value); }
        }


        /// <inheritdoc />
        public virtual bool PerformingUpdate { get; set; }

        /// <inheritdoc />
        public CodeControlStatus Status { get; set; }

        /// <inheritdoc />
        public double XOffset { get; set; }

        /// <inheritdoc />
        public double OutputWidth { get; set; }

        /// <inheritdoc />
        /// <inheritdoc />
        public double PixelsPerDip { get; set; }


        /// <inheritdoc />
        public Dispatcher SecondaryDispatcher
        {
            get { return RoslynCodeControl.StaticSecondaryDispatcher; }
        }


        /// <inheritdoc />
        public DispatcherOperation<CustomTextSource4> InnerUpdateDispatcherOperation { get; set; }

        /// <inheritdoc />
        public Channel<UpdateInfo> UpdateChannel { get; set; }

        /// <inheritdoc />
        public virtual DocumentPaginator DocumentPaginator => new RoslynPaginator(this);


        /// <inheritdoc />
        public ScrollViewer _scrollViewer { get; set; }

        /// <inheritdoc />
        public CustomTextSource4 CustomTextSource { get; set; }

        /// <inheritdoc />
        public virtual bool InitialUpdate { get; set; }

        /// <inheritdoc />
        public virtual int InsertionPoint { get; set; }

        /// <inheritdoc />
        public virtual CharInfo InsertionCharInfo { get; set; }

        public TextSourceInitializationParameters CreateDefaultTextSourceArguments()
        {
            var emSize0 = FontSize;
            var tree = SyntaxTree;
            SyntaxNode node0 = null;
            if (tree != null) node0 = tree.GetRoot();

            Compilation compilation = null;
            var n1 = FontFamily.FamilyNames[XmlLanguage.GetLanguage("en-US")];
            var tf = new Typeface(new FontFamily(n1), FontStyles.Normal, FontWeights.Normal,
                FontStretches.Normal);
            Debug.WriteLine($"{n1} {emSize0}");

            return new TextSourceInitializationParameters(PixelsPerDip, emSize0, tree, node0, compilation, tf);
        }


        /// <inheritdoc />
        public LinkedList<CharInfo> CharInfos { get; set; }

        /// <inheritdoc />
        public Task<CustomTextSource4> InnerUpdate(MainUpdateParameters mainUpdateParameters,
            TextSourceInitializationParameters textSourceInitializationParameters)
        {
            return CommonText.InnerUpdate(mainUpdateParameters,
                () => CreateCustomTextSource4(textSourceInitializationParameters));
        }

        protected static CustomTextSource4 CreateCustomTextSource4(
            TextSourceInitializationParameters p)
        {
            var customTextSource4 =
                CommonText.CreateAndInitTextSource(p.PixelsPerDip, p.Tf,
                    p.Tree, p.Node0,
                    p.Compilation, p.EmSize0);
            return customTextSource4;
        }


        /// <summary>
        /// 
        /// </summary>
        public static readonly DependencyProperty SyntaxNodeProperty = RoslynProperties.SyntaxNodeProperty;

        /// <summary>
        /// 
        /// </summary>
        public SyntaxNode SyntaxNode
        {
            get { return (SyntaxNode) GetValue(SyntaxNodeProperty); }
            set { SetValue(SyntaxNodeProperty, value); }
        }

        /// <summary>
        /// 
        /// </summary>
        public static readonly DependencyProperty SyntaxTreeProperty = RoslynProperties.SyntaxTreeProperty;

        /// <summary>
        /// 
        /// </summary>
        public SyntaxTree SyntaxTree
        {
            get { return (SyntaxTree) GetValue(SyntaxTreeProperty); }
            set { SetValue(SyntaxTreeProperty, value); }
        }

        public static readonly DependencyProperty SourceTextProperty = RoslynProperties.SourceTextProperty;

        public static readonly DependencyProperty SemanticModelProperty = RoslynProperties.SemanticModelProperty;
        private readonly ChannelReader<UpdateInfo> _reader;
        public JoinableTaskFactory JTF{ get; set; }
        public virtual JoinableTaskFactory JTF2{ get; set; }

        public SemanticModel SemanticModel
        {
            get { return (SemanticModel) GetValue(SemanticModelProperty); }
            set { SetValue(SemanticModelProperty, value); }
        }

        /// <summary>
        /// 
        /// </summary>
        public string SourceText
        {
            get { return (string) GetValue(SourceTextProperty); }
            set { SetValue(SourceTextProperty, value); }
        }

        /// <inheritdoc />
        public async Task UpdateFormattedTextAsync()
        {
            CustomTextSource4 ret;
            Debug.WriteLine("Entering updateformattedtext " + ((ICodeView) this).PerformingUpdate);
            if (((ICodeView) this).PerformingUpdate)
            {
                Debug.WriteLine("Already performing update");
                ret = null;
            }
            else
            {
                var runTask = JTF.RunAsync(ReaderListenerAsync);

                ((ICodeView) this).PerformingUpdate = true;
                ((ICodeView) this).Status = CodeControlStatus.Rendering;
                ((ICodeView) this).RaiseEvent(new RoutedEventArgs(RoslynCodeControl.RenderStartEvent, this));

                var textStorePosition = 0;
                var linePosition = new Point(((ICodeView) this).XOffset, 0);

                ((ICodeView) this).TextDestination.Children.Clear();

                var line = 0;

                Debug.WriteLine("Calling inner update");
                // _scrollViewer.VerticalScrollBarVisibility = ScrollBarVisibility.Disabled;
                var fontFamilyFamilyName = ((ICodeView) this).FontFamily.FamilyNames[XmlLanguage.GetLanguage("en-US")];
                Debug.WriteLine(fontFamilyFamilyName);
                Debug.WriteLine("OutputWidth " + ((ICodeView) this).OutputWidth);
                // not sure what to do here !!
                // Rectangle.Width = OutputWidth + Rectangle.StrokeThickness * 2;
                var emSize = ((ICodeView) this).FontSize;
                var fontWeight = ((ICodeView) this).FontWeight;
                var customTextSource4Parameters = ((ICodeView) this).CreateDefaultTextSourceArguments();
                var mainUpdateParameters = new MainUpdateParameters(textStorePosition, line, linePosition,
                    CommonText.Formatter, ((ICodeView) this).OutputWidth, ((ICodeView) this).PixelsPerDip, emSize,
                    fontFamilyFamilyName, ((ICodeView) this).UpdateChannel.Writer, fontWeight,
                    null, customTextSource4Parameters);

                await JTF2.SwitchToMainThreadAsync();

                SecondaryThreadTasks();
                // var dispatcherOperation = ((IFace1) this).SecondaryDispatcher.InvokeAsync(async () =>
                // {
                var rr = ((ICodeView) this).InnerUpdate(mainUpdateParameters, customTextSource4Parameters);
                var src = await rr;
                // return src;
                // }
                // );

                await JTF.SwitchToMainThreadAsync();

                ((ICodeView) this).InnerUpdateDispatcherOperation = null;
                ((ICodeView) this).CustomTextSource = src;
                Debug.WriteLine("Return from await inner update");

                ((ICodeView) this).PerformingUpdate = false;
                ((ICodeView) this).InitialUpdate = false;
                ((ICodeView) this).RaiseEvent(new RoutedEventArgs(RoslynCodeControl.RenderCompleteEvent, this));
                ((ICodeView) this).Status = CodeControlStatus.Rendered;
              
            }
        }

        protected virtual async Task SecondaryThreadTasks()
        {
        }

        private async Task ReaderListenerAsync()
        {

            var ui = await _reader.ReadAsync();
            
            // fixme
            //CharInfos.AddRange(ui.CharInfos);
            var dg = ui.DrawingGroup;
            var dg2 = new DrawingGroup();
            foreach (var dgChild in dg.Children)
                dg2.Children.Add(dgChild);
            TextDestination.Children.Add(dg2);
            var uiRect = dg2.Bounds;
            Debug.WriteLine($"UIRect is {uiRect}");
            var maxY = Math.Max(MaxY, uiRect.Bottom);
            MaxY = maxY;
            var maxX = Math.Max(MaxX, uiRect.Right);
            MaxX = maxX;
            // bound to viewbox height / width
            // Rectangle.Height = maxY;
            // Rectangle.Width = maxX;
            var boundsLeft = Math.Min(TextDestination.Bounds.Left, 0);
            boundsLeft -= 3;
            var boundsTop = Math.Min(TextDestination.Bounds.Top, 0);
            boundsTop -= 3;
            DrawingBrushViewbox = new Rect(boundsLeft, boundsTop, maxX, maxY);
            await ReaderListenerAsync();
        }

        public virtual DrawingGroup TextDestination { get; set; }
        public string DocumentTitle { get; set; }
    }
}