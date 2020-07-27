using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using JetBrains.Annotations;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.VisualStudio.Threading;
// ReSharper disable VirtualMemberNeverOverridden.Global
// ReSharper disable UnusedParameter.Global
#pragma warning disable 8603
#pragma warning disable 8625
#pragma warning disable 8604
#pragma warning disable 8618
#pragma warning disable 8600

namespace RoslynCodeControls
{
    public class RoslynCodeBase : Control, ICodeView, IDocumentPaginatorSource, INotifyPropertyChanged
    {
        public static readonly DependencyProperty LengthProperty = DependencyProperty.Register("Length", typeof(int), typeof(RoslynCodeControl), new PropertyMetadata(default(int)));

        public delegate void DebugDelegate(string msg, int debugLevel=0);
        // ReSharper disable once UnusedMember.Global
        public RoslynCodeBase() : this(null)
        {
        }

        public RoslynCodeBase([CanBeNull] DebugDelegate debugOut = null)
        {
#if DEBUG
            _debugFn = debugOut ?? DebugFn0;

#else
            _debugFn = debugOut ?? ((s,i) => { });
#endif

            TextDestination = new DrawingGroup();
            DrawingBrush = new DrawingBrush();
            UpdateChannel = Channel.CreateUnbounded<UpdateInfo>(new UnboundedChannelOptions()
                {SingleReader = true, SingleWriter = true});
            PixelsPerDip = 1.0;
            OutputWidth = 6.5 * 96;
        }

        private void DebugFn0(string s, int debugLevel)
        {
            if (DebugLevel < debugLevel) return;
            var t = Thread.CurrentThread;
            var newMsg = $"{t.Name} {t.ManagedThreadId}: {Task.CurrentId}: {s}";
            Debug.WriteLine(newMsg);
            ProtoLogger.Instance.LogAction(newMsg);
        }

        public int DebugLevel { get; set; }

        public static readonly RoutedEvent RenderCompleteEvent = EventManager.RegisterRoutedEvent("RenderComplete",
            RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(RoslynCodeControl));

        public static readonly RoutedEvent RenderStartEvent = EventManager.RegisterRoutedEvent("RenderStart",
            RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(RoslynCodeControl));

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

        protected virtual void OnSourceTextChanged1(string newValue, string eOldValue)
        {
            if (ChangingText || UpdatingSourceText)
                return;
            if (newValue == null) return;
            UpdatingSourceText = true;
            DebugFn("Source text changed, creating compilation etc");
            var compilation = CSharpCompilation.Create(
                "test",
                new[]
                {
                    SyntaxFactory.ParseSyntaxTree(
                        newValue)
                }, new[] {MetadataReference.CreateFromFile(typeof(object).Assembly.Location)});
            Compilation = compilation;
            SyntaxTree = compilation.SyntaxTrees.First();

            SyntaxNode = SyntaxTree.GetRoot();
            SemanticModel = compilation.GetSemanticModel(SyntaxTree);
            UpdatingSourceText = false;
        }

        protected bool UpdatingSourceText { get; private set; }

        protected bool ChangingText { get; set; }

        public virtual Rect DrawingBrushViewbox { get; set; }

        public double MaxX { get; protected set; }

        public double MaxY { get; protected set; }

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
        public double XOffset { get; set; } = 20;

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
        public Channel<UpdateInfo> UpdateChannel { get; set; }

        public virtual DocumentPaginator DocumentPaginator
        {
            get { return new RoslynPaginator(this); }
        }


        /// <inheritdoc />
        public ScrollViewer ScrollViewer { get; set; }

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
            // _debugFn?.Invoke($"{n1} {emSize0}");

            return new TextSourceInitializationParameters(PixelsPerDip, emSize0, tree, node0, compilation, tf,
                _debugFn);
        }


        /// <inheritdoc />
        public LinkedList<CharInfo> CharInfos { get; set; } = new LinkedList<CharInfo>();

        /// <inheritdoc />
        public CustomTextSource4 InnerUpdate(MainUpdateParameters mainUpdateParameters,
            TextSourceInitializationParameters textSourceInitializationParameters)
        {
            return CommonText.InnerUpdateAsync(mainUpdateParameters,
                () => CreateCustomTextSource4(textSourceInitializationParameters));
        }

        private static CustomTextSource4 CreateCustomTextSource4(
            TextSourceInitializationParameters p)
        {
            var customTextSource4 =
                CommonText.CreateAndInitTextSource(p.PixelsPerDip, p.Tf,
                    p.Tree, p.Node0,
                    p.Compilation, p.EmSize0, p.DebugFn);
            customTextSource4.CurrentRendering = FontRendering.CreateInstance(p.EmSize0,
                TextAlignment.Left,
                new TextDecorationCollection(), Brushes.Black, p.Tf);

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
        protected readonly DebugDelegate _debugFn;
        private Rectangle _rectangle;
        private int _length;

        public virtual void DebugFn(string msg, int debugLevel=10)
        {
#if DEBUG
            _debugFn?.Invoke(msg, debugLevel);
#endif
        }

        public virtual JoinableTaskFactory JTF { get; } = new JoinableTaskFactory(new JoinableTaskContext());
        public virtual JoinableTaskFactory JTF2 { get; set; }

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
            var codeView = (ICodeView) this;
            _debugFn?.Invoke($"Entering {nameof(UpdateFormattedTextAsync)} {codeView.PerformingUpdate}");
            if (codeView.PerformingUpdate)
            {
                _debugFn?.Invoke("Already performing update");
                
            }
            else
            {
                JTF.RunAsync(ReaderListenerAsync);

                codeView.PerformingUpdate = true;
                codeView.Status = CodeControlStatus.Rendering;
                codeView.Reset();
                codeView.RaiseEvent(new RoutedEventArgs(RenderStartEvent, this));

                var textStorePosition = 0;
                var linePosition = new Point(codeView.XOffset, 0);

                codeView.TextDestination.Children.Clear();

                var line = 0;

                _debugFn?.Invoke("Calling inner update");
                // ScrollViewer.VerticalScrollBarVisibility = ScrollBarVisibility.Disabled;
                var fontFamilyFamilyName = codeView.FontFamily.FamilyNames[XmlLanguage.GetLanguage("en-US")];
                _debugFn?.Invoke(fontFamilyFamilyName);
                _debugFn?.Invoke("OutputWidth " + codeView.OutputWidth);
                // not sure what to do here !!
                // Rectangle.Width = OutputWidth + Rectangle.StrokeThickness * 2;
                var emSize = codeView.FontSize;
                var fontWeight = codeView.FontWeight;
                var customTextSource4Parameters = codeView.CreateDefaultTextSourceArguments();
                var mainUpdateParameters = new MainUpdateParameters(textStorePosition, line, linePosition,
                    CommonText.Formatter, codeView.OutputWidth, codeView.PixelsPerDip, emSize,
                    fontFamilyFamilyName, codeView.UpdateChannel.Writer, fontWeight,
                    null, customTextSource4Parameters, _debugFn);

                await JTF2.SwitchToMainThreadAsync();

                SecondaryThreadTasks();
                var source = codeView.InnerUpdate(mainUpdateParameters, customTextSource4Parameters);
                await JTF.SwitchToMainThreadAsync();

                codeView.CustomTextSource = source;
                _debugFn?.Invoke("Return from await inner update");

                codeView.PerformingUpdate = false;
                codeView.InitialUpdate = false;
                codeView.RaiseEvent(new RoutedEventArgs(RenderCompleteEvent, this));
                codeView.Status = CodeControlStatus.Rendered;
                codeView.InsertionPoint = 0;
                codeView.InsertionLineNode = codeView.FindLine(0);
                await UpdateRoslynPropertiesAsync();
            }
        }

        protected virtual void SecondaryThreadTasks()
        {
        }

        private async Task ReaderListenerAsync()
        {
            while (!UpdateChannel.Reader.Completion.IsCompleted)
            {
                var ui = await UpdateChannel.Reader.ReadAsync();
                for (var i = 0; i < ui.NumLineInfos; i++)
                {
                    LineInfos2.AddLast(ui.LineInfos[i]);

                }
                // fixme
                //CharInfos.AddRange(ui.CharInfos);
                var dg = ui.DrawingGroup;
                var dg2 = new DrawingGroup();
                foreach (var dgChild in dg.Children) dg2.Children.Add(dgChild);
                TextDestination.Children.Add(dg2);
                var uiRect = dg2.Bounds;
                _debugFn?.Invoke($"UIRect is {uiRect}");
                var maxY = Math.Max(MaxY, uiRect.Bottom);
                MaxY = maxY;
                var maxX = Math.Max(MaxX, uiRect.Right);
                MaxX = maxX;


                var boundsLeft = Math.Min(TextDestination.Bounds.Left, 0);
                boundsLeft -= 3;
                var boundsTop = Math.Min(TextDestination.Bounds.Top, 0);
                boundsTop -= 3;

                var width = maxX - boundsLeft;
                var height = maxY - boundsTop;
                DrawingBrush.Viewbox = DrawingBrushViewbox =
                    new Rect(boundsLeft, boundsTop, width, height);

                if (Rectangle == null) continue;
                Rectangle.Width = width;
                Rectangle.Height = height;

                
            }
        }

        public virtual LinkedList<LineInfo2> LineInfos2 { get; } = new LinkedList<LineInfo2>();

        public virtual LineInfo2? InsertionLine
        {
            get { return InsertionLineNode?.Value; }
        }

        public virtual Rectangle Rectangle
        {
            get { return _rectangle; }
            set
            {
                if (Equals(_rectangle, value)) return;
                if (_rectangle != null) _rectangle.MouseMove -= RectangleOnMouseMove;
                _rectangle = value;
                if (_rectangle != null) _rectangle.MouseMove += RectangleOnMouseMove;
            }
        }

        protected virtual void RectangleOnMouseMove(object sender, MouseEventArgs e)
        {
        }

        public virtual DrawingGroup TextDestination { get; set; }
        public string DocumentTitle { get; set; }
        public virtual LinkedListNode<LineInfo2> InsertionLineNode { get; set; }

        public virtual LinkedListNode<LineInfo2> FindLine(int lineNo, LinkedListNode<LineInfo2> startNode = null)
        {
            var li0 = startNode ?? LineInfos2.First;
            for (; li0 != null; li0 = li0.Next)
                if (li0.Value.LineNumber == lineNo)
                    return li0;

            return null;
        }

        /// <inheritdoc />
        public virtual void Reset()
        {
            LineInfos2.Clear();
            Rectangle.Width = 0;
            Rectangle.Height = 0;

        }

        public static readonly DependencyProperty TextSourceTextProperty = DependencyProperty.Register(
            "TextSourceText", typeof(string), typeof(RoslynCodeControl),
            new PropertyMetadata(default(string)));

        public string TextSourceText
        {
            get { return (string)GetValue(TextSourceTextProperty); }
            set { SetValue(TextSourceTextProperty, value); }
        }


        protected virtual async Task UpdateRoslynPropertiesAsync()
        {
            if (CustomTextSource == null)
            {
                DebugFn("Text source is null");
            }
            else
            {
                ChangingText = true;

                SyntaxNode = CustomTextSource.Node;
                SyntaxTree = CustomTextSource.Tree;
                TextSourceText = CustomTextSource.Text.ToString();
                var doc = Document.WithSyntaxRoot(SyntaxNode);
                var sm = await doc.GetSemanticModelAsync();
                if (sm != null) Compilation = sm.Compilation;
                Length = CustomTextSource.Length;
                Document = doc;
                SemanticModel = sm;
                ChangingText = false;
                DebugFn("Finished updating roslyn properties.");
                RaiseEvent(new ContentChangedRoutedEventArgs(this));
            }
        }
        public int Length
        {
            get { return (int)GetValue(LengthProperty); }
            set { SetValue(LengthProperty, value); }
        }


        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}