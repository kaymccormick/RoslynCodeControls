using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Media.TextFormatting;
using System.Windows.Shapes;
using System.Windows.Threading;
using JetBrains.Annotations;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace RoslynCodeControls
{
    /// <summary>
    /// 
    /// </summary>
//    [TitleMetadata("Formatted Code Control")]
    public class RoslynCodeControl : SyntaxNodeControl, ILineDrawer, INotifyPropertyChanged
    {
        public static readonly RoutedEvent RenderCompleteEvent = EventManager.RegisterRoutedEvent("RenderComplete",
            RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(RoslynCodeControl));

        public static readonly RoutedEvent RenderStartEvent = EventManager.RegisterRoutedEvent("RenderStart",
            RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(RoslynCodeControl));

        public static readonly DependencyProperty InsertionPointProperty = DependencyProperty.Register(
            "InsertionPoint", typeof(int), typeof(RoslynCodeControl),
            new PropertyMetadata(default(int), OnInsertionPointChanged));

        public int InsertionPoint
        {
            get { return (int) GetValue(InsertionPointProperty); }
            set { SetValue(InsertionPointProperty, value); }
        }

        private static void OnInsertionPointChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((RoslynCodeControl) d).OnInsertionPointChanged((int) e.OldValue, (int) e.NewValue);
        }

        public ISymbol EnclosingSymbol
        {
            get { return _enclosingSymbol; }
            set
            {
                if (Equals(value, _enclosingSymbol)) return;
                _enclosingSymbol = value;
                OnPropertyChanged();
            }
        }

        protected virtual async void OnInsertionPointChanged(int oldValue, int newValue)
        {
            UpdateCaretPosition();
            try
            {
                var enclosingsymbol = Model?.GetEnclosingSymbol(newValue);
                EnclosingSymbol = enclosingsymbol;

                Debug.WriteLine(EnclosingSymbol?.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat));
                if (Model != null && InsertionRegion.SyntaxNode != null)
                {
                    var ti = Model.GetTypeInfo(InsertionRegion.SyntaxNode);
                    if (ti.Type != null)
                        Debug.WriteLine(ti.Type.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat));
                    ;
                }
            }
            catch (Exception ex)
            {
            }

            // var completionService = CompletionService.GetService(Document);
            // var results = await completionService.GetCompletionsAsync(Document, InsertionPoint);
            // foreach (var completionItem in results.Items)
            // {
            // Debug.WriteLine(completionItem.DisplayText);
            // Debug.WriteLine(completionItem.InlineDescription);
            // }
        }


        /// <summary>
        /// 
        /// </summary>
        public static readonly DependencyProperty HoverOffsetProperty = DependencyProperty.Register(
            "HoverOffset", typeof(int), typeof(RoslynCodeControl), new PropertyMetadata(default(int)));

        /// <summary>
        /// 
        /// </summary>
        public static readonly DependencyProperty HoverRegionInfoProperty = DependencyProperty.Register(
            "HoverRegionInfo", typeof(RegionInfo), typeof(RoslynCodeControl),
            new PropertyMetadata(default(RegionInfo)));

        /// <summary>
        /// 
        /// </summary>
        public RegionInfo HoverRegionInfo
        {
            get { return (RegionInfo) GetValue(HoverRegionInfoProperty); }
            set { SetValue(HoverRegionInfoProperty, value); }
        }

        /// <inheritdoc />
        public override void EndInit()
        {
            base.EndInit();
        }

        /// <summary>
        /// 
        /// </summary>
        public int HoverOffset
        {
            get { return (int) GetValue(HoverOffsetProperty); }
            set { SetValue(HoverOffsetProperty, value); }
        }

        /// <summary>
        /// 
        /// </summary>
        public static readonly DependencyProperty HoverTokenProperty = DependencyProperty.Register(
            "HoverToken", typeof(SyntaxToken?), typeof(RoslynCodeControl),
            new PropertyMetadata(default(SyntaxToken?)));

        /// <summary>
        /// 
        /// </summary>
        public static readonly DependencyProperty HoverSymbolProperty = DependencyProperty.Register(
            "HoverSymbol", typeof(ISymbol), typeof(RoslynCodeControl), new PropertyMetadata(default(ISymbol)));

        /// <summary>
        /// 
        /// </summary>
        public ISymbol HoverSymbol
        {
            get { return (ISymbol) GetValue(HoverSymbolProperty); }
            set { SetValue(HoverSymbolProperty, value); }
        }

        /// <summary>
        /// 
        /// </summary>
        public SyntaxToken? HoverToken
        {
            get { return (SyntaxToken?) GetValue(HoverTokenProperty); }
            set { SetValue(HoverTokenProperty, value); }
        }

        /// <summary>
        /// 
        /// </summary>
        public static readonly DependencyProperty HoverSyntaxNodeProperty = DependencyProperty.Register(
            "HoverSyntaxNode", typeof(SyntaxNode), typeof(RoslynCodeControl),
            new PropertyMetadata(default(SyntaxNode), new PropertyChangedCallback(OnHoverSyntaxNodeUpdated)));

        private static void OnHoverSyntaxNodeUpdated(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            Debug.WriteLine(e.NewValue?.ToString());
        }

        /// <summary>
        /// 
        /// </summary>
        public SyntaxNode HoverSyntaxNode
        {
            get { return (SyntaxNode) GetValue(HoverSyntaxNodeProperty); }
            set { SetValue(HoverSyntaxNodeProperty, value); }
        }

        /// <summary>
        /// 
        /// </summary>
        public static readonly DependencyProperty HoverColumnProperty = DependencyProperty.Register(
            "HoverColumn", typeof(int), typeof(RoslynCodeControl), new PropertyMetadata(default(int)));

        /// <summary>
        /// 
        /// </summary>
        public int HoverColumn
        {
            get { return (int) GetValue(HoverColumnProperty); }
            set { SetValue(HoverColumnProperty, value); }
        }

        /// <summary>
        /// 
        /// </summary>
        public static readonly DependencyProperty HoverRowProperty = DependencyProperty.Register(
            "HoverRow", typeof(int), typeof(RoslynCodeControl), new PropertyMetadata(default(int)));

        /// <summary>
        /// 
        /// </summary>
        public int HoverRow
        {
            get { return (int) GetValue(HoverRowProperty); }
            set { SetValue(HoverRowProperty, value); }
        }

        /// <summary>
        /// 
        /// </summary>
        protected bool UiLoaded { get; set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="pixelsPerDip"></param>
        /// <param name="tf"></param>
        /// <param name="st"></param>
        /// <param name="node"></param>
        /// <param name="compilation"></param>
        /// <param name="s1"></param>
        /// <param name="typefaceManager"></param>
        /// <returns></returns>
        protected CustomTextSource4 CreateAndInitTextSource(double pixelsPerDip,
            Typeface tf, SyntaxTree st, SyntaxNode node, Compilation compilation,
            [NotNull] SynchronizationContext synchContext, double fontSize)
        {
            if (synchContext == null) throw new ArgumentNullException(nameof(synchContext));

            if (st == null)
            {
                st = SyntaxFactory.ParseSyntaxTree("");
                node = st.GetRoot();
                compilation = null;
            }

            var textDecorationCollection = new TextDecorationCollection();
            var typeface = tf;
            var fontRendering = FontRendering.CreateInstance(fontSize,
                TextAlignment.Left, textDecorationCollection,
                Brushes.Black, typeface);
            var source = new CustomTextSource4(pixelsPerDip, fontRendering, new GenericTextRunProperties(
                fontRendering,
                PixelsPerDip), synchContext)
            {
                EmSize = fontSize,
                Compilation = compilation,
                Tree = st,
                Node = node
            };
            source.PropertyChanged += SourceOnPropertyChanged;
            source.Init();
            return source;
        }

        public SynchronizationContext SynchContext { get; set; }

        private async void SourceOnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "Text")
            {
                var textSourceText = CustomTextSource.Text.ToString();
                await Dispatcher.InvokeAsync(() => { TextSourceText = textSourceText; });
            }
        }

        public static readonly DependencyProperty TextSourceTextProperty = DependencyProperty.Register(
            "TextSourceText", typeof(string), typeof(RoslynCodeControl),
            new PropertyMetadata(default(string), OnTextSourceTextChanged));

        public string TextSourceText
        {
            get { return (string) GetValue(TextSourceTextProperty); }
            set { SetValue(TextSourceTextProperty, value); }
        }

        private static void OnTextSourceTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((RoslynCodeControl) d).OnTextSourceTextChanged((string) e.OldValue, (string) e.NewValue);
        }


        protected virtual void OnTextSourceTextChanged(string oldValue, string newValue)
        {
        }

        private DrawingBrush _myDrawingBrush = new DrawingBrush();
        private DrawingGroup _textDest = new DrawingGroup();
        private Point _pos;

        /// <summary>
        /// 
        /// </summary>
        protected double MaxX { get; set; }

        /// <summary>
        /// 
        /// </summary>
        protected double MaxY { get; set; }


        // ReSharper disable once UnusedMember.Local
        private void UpdateCompilation(Compilation compilation)
        {
            HandleDiagnostics(compilation.GetDiagnostics());
        }

        private void HandleDiagnostics(ImmutableArray<Diagnostic> diagnostics)
        {
            foreach (var diagnostic in diagnostics)
            {
                Debug.WriteLine(diagnostic.ToString());
                MarkLocation(diagnostic.Location);
                if (diagnostic.Severity == DiagnosticSeverity.Error) Errors.Add(new DiagnosticError(diagnostic));
            }
        }

        /// <summary>
        /// 
        /// </summary>
        private List<CompilationError> Errors { get; } = new List<CompilationError>();

        private void MarkLocation(Location diagnosticLocation)
        {
            switch (diagnosticLocation.Kind)
            {
                case LocationKind.SourceFile:
                    if (diagnosticLocation.SourceTree == SyntaxTree)
                    {
                        // ReSharper disable once UnusedVariable
                        var s = diagnosticLocation.SourceSpan.Start;
                    }

                    break;
            }
        }

#if false
        protected override Size MeasureOverride(Size constraint)
        {
            _grid.Measure(constraint);
            var gridDesiredSize = _grid.DesiredSize;
            Debug.WriteLine(gridDesiredSize.ToString());
            return gridDesiredSize;
            Debug.WriteLine(constraint.ToString());
            return base.MeasureOverride(constraint);
            return new Size(max_x, _pos.Y);
        }
#endif

        static RoslynCodeControl()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(RoslynCodeControl),
                new FrameworkPropertyMetadata(typeof(RoslynCodeControl)));
            SyntaxTreeProperty.OverrideMetadata(typeof(RoslynCodeControl), new FrameworkPropertyMetadata(default(SyntaxTree), FrameworkPropertyMetadataOptions.None, OnSyntaxTreeChanged_));
            SyntaxNodeProperty.OverrideMetadata(typeof(RoslynCodeControl), new PropertyMetadata(default(SyntaxNode), OnNodeUpdated));
            // TextElement.FontFamilyProperty.OverrideMetadata(typeof(RoslynCodeControl), new PropertyMetadata(null, PropertyChangedCallback));
            // TextElement.FontSizeProperty.OverrideMetadata(typeof(RoslynCodeControl), new PropertyMetadata(16.0, PropertyChangedCallback2));
        }

        private static void OnSyntaxTreeChanged_(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var ss = (RoslynCodeControl)d;
            ss.OnSyntaxTreeUpdated((SyntaxTree)e.NewValue);
        }

        private static async void PropertyChangedCallback2(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            Debug.WriteLine(e.NewValue.ToString());
            //    await ((RoslynCodeControl)d).UpdateTextSource();
        }

        private static async void PropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            //   await ((RoslynCodeControl) d).UpdateTextSource();
        }

        /// <summary>
        /// 
        /// </summary>
        public RoslynCodeControl()

        {
            _channel = Channel.CreateUnbounded<UpdateInfo>(new UnboundedChannelOptions()
                {SingleReader = true, SingleWriter = true});
            _reader = _channel.Reader;
            _reader.ReadAsync().AsTask().ContinueWith(ContinuationFunction, CancellationToken.None,
                TaskContinuationOptions.None, TaskScheduler.FromCurrentSynchronizationContext());
            var xx = new DoubleAnimationUsingPath();
            _x1 = new ObjectAnimationUsingKeyFrames();
            _x1.RepeatBehavior = RepeatBehavior.Forever;
            _x1.Duration = new Duration(TimeSpan.FromSeconds(1));
            Debug.WriteLine(_x1.Duration.ToString());

            var c = new ObjectKeyFrameCollection();
            c.Add(new DiscreteObjectKeyFrame(Visibility.Visible));
            c.Add(new DiscreteObjectKeyFrame(Visibility.Hidden, KeyTime.FromPercent(.6)));
            c.Add(new DiscreteObjectKeyFrame(Visibility.Visible, KeyTime.FromPercent(.4)));
            _x1.KeyFrames = c;


            CSharpCompilationOptions = new CSharpCompilationOptions(default(OutputKind));
            PixelsPerDip = VisualTreeHelper.GetDpi(this).PixelsPerDip;
            // CommandBindings.Add(new CommandBinding(WpfAppCommands.SerializeContents, Executed));
            // CommandBindings.Add(new CommandBinding(WpfAppCommands.Compile, CompileExecuted));
            CommandBindings.Add(new CommandBinding(EditingCommands.EnterLineBreak, OnEnterLineBreak,
                CanEnterLineBreak));

            ;
            InputBindings.Add(new KeyBinding(EditingCommands.EnterLineBreak, Key.Enter, ModifierKeys.None));


        }

        private void ContinuationFunction(Task<UpdateInfo> z)
        {
            var ui = z.Result;
            var dc = _textDest.Append();
            dc.DrawImage(ui.ImageSource, ui.Rect);
            dc.Close();


            // if (w >= roslynCodeControl.MaxX) roslynCodeControl.MaxX = w;

            // var rectangleWidth = roslynCodeControl.MaxX + roslynCodeControl._xOffset;
            // roslynCodeControl._rectangle.Width = rectangleWidth;

            _rectangle.Height = ui.Rect.Bottom;
            _myDrawingBrush.Viewbox = new Rect(0, 0, _rectangle.ActualWidth, ui.Rect.Bottom);
            _myDrawingBrush.ViewboxUnits = BrushMappingMode.Absolute;
            _reader.ReadAsync().AsTask().ContinueWith(ContinuationFunction, CancellationToken.None,
                TaskContinuationOptions.None, TaskScheduler.FromCurrentSynchronizationContext());
        }

        // private void CompileExecuted(object sender, ExecutedRoutedEventArgs e)
        // {
        //     if (Document != null)
        //         Document.Project.GetCompilationAsync().ContinueWith(task =>
        //         {
        //             Dispatcher.InvokeAsync(() => Compilation = task.Result);
        //         });
        // }

        // public static RoutedEvent ErrorEvent = EventManager.RegisterRoutedEvent(typeof(RoslynCodeControl))

        private async void OnEnterLineBreak(object sender, ExecutedRoutedEventArgs e)
        {
            var b = await DoInput("\r\n");
            if (!b)
                Debug.WriteLine("Newline failed");
            // ChangingText = true;
            // Debug.WriteLine("Enter line break");
            // InsertionPoint = TextSource.EnterLineBreak(InsertionPoint);

            // SourceText += "\r\n";
            // ChangingText = false;
        }

        private void CanEnterLineBreak(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
            e.Handled = true;
        }

        private async void Executed(object sender, ExecutedRoutedEventArgs e)
        {
            var text = await SyntaxTree.GetTextAsync();
            var root = await SyntaxTree.GetRootAsync();
            using (var s = new FileStream(@"C:\temp\serialize.bin", FileMode.Create))

            {
                root.SerializeTo(s);
            }
        }

        /// <inheritdoc />
        protected virtual void OnSyntaxTreeUpdated(SyntaxTree newValue)
        {
            _text = newValue.GetText();
            if (UpdatingSourceText)
                SourceText = _text.ToString();
        }

        /// <inheritdoc />
        protected override async void OnSourceTextChanged1(string newValue, string eOldValue)
        {
            base.OnSourceTextChanged1(newValue, eOldValue);

            if (newValue != null && !ChangingText) await UpdateTextSource();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <exception cref="InvalidOperationException"></exception>
        public async Task UpdateTextSource()
        {
            if (!UiLoaded)
                return;
            if (Compilation != null && Compilation.SyntaxTrees.Contains(SyntaxTree) == false)
            {
                throw new InvalidOperationException();
                Compilation = null;
                Debug.WriteLine("Compilation does not contain syntax tree.");
            }

            if (SyntaxNode == null || SyntaxTree == null) return;
            if (ReferenceEquals(SyntaxNode.SyntaxTree, SyntaxTree) == false)
                throw new InvalidOperationException("SyntaxNode is not within syntax tree");

            //_errorTextSource = Errors.Any() ? new ErrorsTextSource(PixelsPerDip, Errors, TypefaceManager) : null;
            //_baseProps = TextSource.BaseProps;
            await UpdateFormattedText();
        }

        /// <summary>
        /// 
        /// </summary>
        private double PixelsPerDip { get; }

        private GeometryDrawing _geometryDrawing;
        private Rect _rect;

        /// <inheritdoc />
        protected override Size MeasureOverride(Size constraint)
        {
            var measureOverride = base.MeasureOverride(constraint);
            var w = _scrollViewer.DesiredSize.Width;
            return measureOverride;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <inheritdoc />
        public override async void OnApplyTemplate()
        {
            _scrollViewer = (ScrollViewer) GetTemplateChild("ScrollViewer");
            if (_scrollViewer != null) OutputWidth = _scrollViewer.ActualWidth;
            // if(OutputWidth == 0)
            // {
            // throw new InvalidOperationException();
            // }

            Debug.WriteLine(OutputWidth.ToString());
            CodeControl = (RoslynCodeControl) GetTemplateChild("CodeControl");
            _rectangle = (Rectangle) GetTemplateChild("Rectangle");
            RegionDG = (DrawingGroup) GetTemplateChild("Region");
            var dpd = DependencyPropertyDescriptor.FromProperty(TextElement.FontSizeProperty, typeof(Rectangle));
            var dpd2 = DependencyPropertyDescriptor.FromProperty(TextElement.FontFamilyProperty, typeof(Rectangle));
            Translate = (TranslateTransform) GetTemplateChild("TranslateTransform");

            if (_rectangle != null)
            {
                // dpd.AddValueChanged(_rectangle, Handler);
                // dpd2.AddValueChanged(_rectangle, Handler2);
            }

            _grid = (Grid) GetTemplateChild("Grid");
            _canvas = (Canvas) GetTemplateChild("Canvas");
            _innerGrid = (Grid) GetTemplateChild("InnerGrid");
            // var tryGetGlyphTypeface = Typeface.TryGetGlyphTypeface(out var gf);

            _textCaret = new TextCaret(20);


            _canvas.Children.Add(_textCaret);


            _border = (Border) GetTemplateChild("Border");
            _myDrawingBrush = (DrawingBrush) GetTemplateChild("DrawingBrush");

            _textDest = (DrawingGroup) GetTemplateChild("TextDest");
            _rect2 = (Rectangle) GetTemplateChild("Rect2");
            _dg2 = (DrawingGroup) GetTemplateChild("DG2");
            UiLoaded = true;

            // StartSecondaryThread();
            //if (TextSource != null) UpdateFormattedText();
        }

        public DrawingGroup RegionDG { get; set; }

        public static void StartSecondaryThread()
        {
            var t = new ThreadStart(SecondaryThreadStart);
            var newWindowThread = SecondaryThread = new Thread(t);
            newWindowThread.SetApartmentState(ApartmentState.STA);
            newWindowThread.Name = "SecondaryThread";
            newWindowThread.IsBackground = true;
            newWindowThread.Start();
        }

        public static Thread SecondaryThread { get; set; }

        public TranslateTransform Translate { get; set; }

        private static void SecondaryThreadStart()
        {
            var d = Dispatcher.CurrentDispatcher;
            // Dispatcher.Invoke(() =>
            // {
            SecondaryDispatcher1 = d;
            // });
            Dispatcher.Run();
        }

        public static Dispatcher SecondaryDispatcher1 { get; set; }

        public Dispatcher SecondaryDispatcher
        {
            get { return SecondaryDispatcher1; }
        }

        private async Task DoUpdateTextSource()
        {
            await UpdateTextSource();
        }


        public RoslynCodeControl CodeControl { get; set; }


        /// <summary>
        /// 
        /// </summary>
        public LineInfo InsertionLine { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public CharacterCell InsertionCharacter { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public RegionInfo InsertionRegion { get; set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="e"></param>
        protected override void OnPreviewKeyDown(KeyEventArgs e)
        {
            base.OnPreviewKeyDown(e);
            switch (e.Key)
            {
                case Key.Left:
                {
                    var ip = --InsertionPoint;
                    if (ip < 0) ip = 0;
                    Debug.WriteLine($"{ip}");

                    if (InsertionCharacter != null)
                    {
                        var newc = InsertionCharacter.PreviousCell;
                        if (newc?.Region != InsertionRegion)
                        {
                            InsertionRegion = newc.Region;
                            if (newc.Region.Line != InsertionLine) InsertionLine = newc.Region.Line;
                        }

                        InsertionCharacter = newc;
                    }

                    var top = InsertionLine.Origin.Y;
                    Debug.WriteLine("Setting top to " + top);

                    _textCaret.SetValue(Canvas.TopProperty, top);
                    if (InsertionCharacter != null)
                        _textCaret.SetValue(Canvas.LeftProperty, InsertionCharacter.Bounds.Left);
                }
                    break;
                case Key.Right:
                {
                    Debug.WriteLine("incrementing insertion point");
                    e.Handled = true;
                    if (InsertionCharacter != null && InsertionCharacter.NextCell == null)
                        break;
                    var ip = ++InsertionPoint;
                    Debug.WriteLine($"Insertion point: {ip}");

                    var newc = InsertionCharacter.NextCell;
                    if (newc.Region != null && newc.Region != InsertionRegion)
                    {
                        InsertionRegion = newc.Region;
                        if (newc.Region.Line != InsertionLine) InsertionLine = newc.Region.Line;
                    }

                    InsertionCharacter = newc;

                    var top = InsertionLine.Origin.Y;
                    Debug.WriteLine("Setting top to " + top);

                    _textCaret.SetValue(Canvas.TopProperty, top);
                    _textCaret.SetValue(Canvas.LeftProperty, InsertionCharacter.Bounds.Left);


                    break;
                }
            }
        }

        /// <inheritdoc />
        protected override async void OnPreviewTextInput(TextCompositionEventArgs e)
        {
            base.OnPreviewTextInput(e);
            var eText = e.Text;
            e.Handled = true;
            try
            {
                await DoInput(eText);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
            }
        }

        public async Task<bool> DoInput(string eText)
        {
            try
            {
                if (CustomTextSource == null) await UpdateTextSource();
                Debug.WriteLine(eText);
                //  if (_textDest.Children.Count == 0) _textDest.Children.Add(new DrawingGroup());

                var insertionPoint = InsertionPoint;
                var prev = SourceText.Substring(0, insertionPoint);
                var next = SourceText.Substring(insertionPoint);
                var code = prev + eText + next;
                if (InsertionLine != null)
                {
#if false
                var l = InsertionLine.Text.Substring(0, InsertionPoint - InsertionLine.Offset) + e.Text;
                var end = InsertionLine.Offset + InsertionLine.Length;
                if (end - InsertionPoint > 0)
                {
                    var start = InsertionPoint - InsertionLine.Offset;
                    var length = end - InsertionPoint;
                    if (start + length > InsertionLine.Text.Length)
                        length = length - (start + length - InsertionLine.Text.Length);
                    l += InsertionLine.Text.Substring(start, length);
                }
#endif
                }

                if (InsertionLine != null && InsertionLine.LineNumber == 1)
                {
                }

                ChangingText = true;
                var insertionLineOffset = InsertionLine?.Offset ?? 0;
                var originY = InsertionLine?.Origin.Y ?? 0;
                var originX = InsertionLine?.Origin.X ?? 0;
                var insertionLineLineNumber = InsertionLine?.LineNumber ?? 0;
                var insertionLine = InsertionLine;

                var l = new List<LineInfo>();


                var d = new DrawingGroup();
                var drawingContext = d.Open();
                var typefaceName = FontFamily.FamilyNames[XmlLanguage.GetLanguage("en-US")];
                ;
                var inn = new InClassName(this, insertionLineLineNumber, insertionLineOffset, originY, originX,
                    insertionLine, Formatter, OutputWidth, null, PixelsPerDip, CustomTextSource, MaxY, MaxX,
                    d, drawingContext) {FontSize = FontSize, FontFamilyName = typefaceName};
                var lineInfo = await SecondaryDispatcher.InvokeAsync(
                    new Func<LineInfo>(() => Callback(inn, insertionPoint, eText)),
                    DispatcherPriority.Send, CancellationToken.None);
                await Dispatcher.InvokeAsync(() =>
                {
                    InsertionPoint = insertionPoint + eText.Length;
                    if (lineInfo == null) throw new InvalidOleVariantTypeException();

                    if (InsertionPoint == lineInfo.Offset + lineInfo.Length)
                        InsertionLine = new LineInfo()
                        {
                            LineNumber = lineInfo.LineNumber + 1, Offset = InsertionPoint,
                            Origin = new Point(0, InsertionLine.Origin.Y + InsertionLine.Height),
                            PrevLine = InsertionLine
                        };
                    else
                        InsertionLine = (LineInfo) lineInfo;


                    if (InsertionLine.Offset + InsertionLine.Length <= insertionPoint)
                    {
                        if (InsertionLine.NextLine != null)
                        {
                            InsertionLine = InsertionLine.NextLine;
                        }
                        else
                        {
                            InsertionLine.NextLine = new LineInfo()
                            {
                                LineNumber = InsertionLine.LineNumber + 1, PrevLine = InsertionLine,
                                Origin = new Point(0, InsertionLine.Origin.Y + InsertionLine.Height),
                                Offset = InsertionLine.Offset + InsertionLine.Length
                            };
                            InsertionLine = InsertionLine.NextLine;
                        }
                    }

                    if (eText.Length == 1)
                    {
                        //_textCaret.SetValue(Canvas.LeftProperty, 0);
                    }

                    //AdvanceInsertionPoint(e.Text.Length);

                    Debug.WriteLine("About to update source text");
                    SourceText = code;
                    Debug.WriteLine("Done updating source text");
                    ChangingText = false;
                });
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
                return false;
            }
        }

        private LineInfo Callback(InClassName inn, int insertionPoin, string eText)
        {
            try
            {
                inn.CurrentRendering = FontRendering.CreateInstance(inn.FontSize, TextAlignment.Left,
                    new TextDecorationCollection(), Brushes.Black,
                    new Typeface(new FontFamily(inn.FontFamilyName), FontStyles.Normal, FontWeights.Normal,
                        FontStretches.Normal));
                CustomTextSource.TextInput(insertionPoin, eText);


                var lineInfo = RedrawLine((InClassName) inn, out var lineCtx);


                return lineInfo;
            }
            catch (Exception ex)

            {
                Debug.WriteLine(ex.ToString());
            }

            return null;
        }


        /// <inheritdoc />
        protected override void OnGotKeyboardFocus(KeyboardFocusChangedEventArgs e)
        {
            base.OnGotKeyboardFocus(e);
            _textCaret.BeginAnimation(VisibilityProperty, _x1);
        }

        /// <inheritdoc />
        protected override void OnLostKeyboardFocus(KeyboardFocusChangedEventArgs e)
        {
            base.OnLostKeyboardFocus(e);
            _textCaret.BeginAnimation(VisibilityProperty, null);
        }

        private static LineInfo RedrawLine(InClassName inClassName, out LineContext lineCtx)
        {
            //if (roslynCodeControl.LineInfos.Count == 0) roslynCodeControl.LineInfos.Add(null);

            LineInfo outLineInfo;
            using (var myTextLine = inClassName.TextFormatter.FormatLine(inClassName.CustomTextSource4,
                inClassName.Offset, inClassName.ParagraphWidth,
                new GenericTextParagraphProperties(inClassName.CurrentRendering, inClassName.PixelsPerDip), null))
            {
                lineCtx = new LineContext()
                {
                    LineNumber = inClassName.LineNo,
                    CurCellRow = inClassName.LineNo,
                    // LineInfo = inClassName.LineInfo,
                    LineOriginPoint = new Point(inClassName.X, inClassName.Y),
                    MyTextLine = myTextLine,
                    MaxX = inClassName.MaxX,
                    MaxY = inClassName.MaxY,
                    TextStorePosition = inClassName.Offset
                };

                var o = lineCtx.LineOriginPoint;
                inClassName.Dc.Dispatcher.Invoke(() => { myTextLine.Draw(inClassName.Dc, o, InvertAxes.None); });
                var regions = new List<RegionInfo>();
                FormattingHelper.HandleTextLine(regions, ref lineCtx, out var lineI, inClassName.RoslynCodeControl);

                inClassName.RoslynCodeControl.Dispatcher.Invoke(() =>
                {
                    if (inClassName.RoslynCodeControl.LineInfos.Count <= inClassName.LineNo)
                        inClassName.RoslynCodeControl.LineInfos.Add(lineI);
                    else
                        inClassName.RoslynCodeControl.LineInfos[inClassName.LineNo] = lineI;
                });
                outLineInfo = lineI;
            }

            Debug.WriteLine(
                $"{inClassName.RoslynCodeControl._rect.Width}x{inClassName.RoslynCodeControl._rect.Height}");
                

            var lineCtxMaxX = lineCtx.MaxX;
            var lineCtxMaxY = lineCtx.MaxY;
            inClassName.RoslynCodeControl.Dispatcher.Invoke(() =>
            {
                inClassName.Dc.Close();
                if (inClassName.RoslynCodeControl._textDest.Children.Count <= inClassName.LineNo)
                    inClassName.RoslynCodeControl._textDest.Children.Add(inClassName.D);
                else
                    inClassName.RoslynCodeControl._textDest.Children[inClassName.LineNo] = inClassName.D;


                inClassName.RoslynCodeControl.MaxX = lineCtxMaxX;

                inClassName.RoslynCodeControl.MaxY = lineCtxMaxY;
                inClassName.RoslynCodeControl._rectangle.Width = lineCtxMaxX;
                inClassName.RoslynCodeControl._rectangle.Height = lineCtxMaxY;
                inClassName.RoslynCodeControl._rect2.Width = lineCtxMaxX;
                inClassName.RoslynCodeControl._rect2.Height = lineCtxMaxY;
                // inClassName.RoslynCodeControl.UpdateCaretPosition();
//                inClassName.RoslynCodeControl.InvalidateVisual();
            });

            return outLineInfo;
        }

        private void 
            AdvanceInsertionPoint(int textLength)
        {
            InsertionPoint += textLength;
        }


        /// <summary>
        /// 
        /// </summary>
        private Typeface CreateTypeface(FontFamily fontFamily, FontStyle fontStyle, FontStretch fontStretch,
            FontWeight fontWeight)
        {
            return new Typeface(fontFamily, fontStyle, fontWeight, fontStretch);
        }

        protected override void OnTemplateChanged(ControlTemplate oldTemplate, ControlTemplate newTemplate)
        {
            base.OnTemplateChanged(oldTemplate, newTemplate);
            Debug.WriteLine($"{newTemplate}");
        }

        private void OnPropertyChangedz(DependencyPropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(e);
            Debug.WriteLine($"{e.Property.Name}");
            if (e.Property.Name == "DesignerView1")
            {
                Debug.WriteLine($"{e.Property.Name} {e.OldValue} = {e.NewValue}");
                foreach (var m in e.NewValue.GetType().GetMethods())
                    Debug.WriteLine(m.ToString());
                foreach (var ii in e.NewValue.GetType().GetInterfaces())
                    Debug.WriteLine(ii.ToString());
            }
            else if (e.Property.Name == "InstanceBuilderContext")
            {
                Debug.WriteLine($"{e.Property.Name} {e.OldValue} = {e.NewValue}");
            }
        }


        /// <summary>
        /// 
        /// </summary>
        protected TextFormatter Formatter { get; set; } = TextFormatter.Create();

        /// <inheritdoc />
        protected override Size ArrangeOverride(Size arrangeBounds)
        {
            _nliens = (int) (arrangeBounds.Height / (FontFamily.LineSpacing * FontSize));
            // Debug.WriteLine("poassible lines " + _nliens.ToString());
            var arrangeOverride = base.ArrangeOverride(arrangeBounds);
            // Debug.WriteLine(arrangeOverride);
            // Debug.WriteLine(_scrollViewer.ActualWidth);
            var sbar = (ScrollBar) _scrollViewer.Template.FindName("PART_VerticalScrollBar", _scrollViewer);

            OutputWidth = _scrollViewer.ActualWidth - sbar.ActualWidth - _rectangle.StrokeThickness * 2;
            if (InitialUpdate)
            {
                if (PerformingUpdate)
                {
                    Debug.WriteLine("already performing update");
                    return arrangeOverride;
                }

                InitialUpdate = false;
                if (SyntaxNode == null)
                    return arrangeBounds;
                Debug.WriteLine("Performing initial update of text");
                var updateFormattedText = UpdateFormattedText();
                UpdateFormattedTestTask = updateFormattedText;
            }

            return arrangeOverride;
        }

        public Task UpdateFormattedTestTask
        {
            get { return _updateFormattedTestTask; }
            set
            {
                if (Equals(value, _updateFormattedTestTask)) return;
                _updateFormattedTestTask = value;
                OnPropertyChanged();
            }
        }

        public bool InitialUpdate { get; set; } = true;

        /// <summary>
        /// 
        /// </summary>
        public virtual async Task UpdateFormattedText()
        {
            if (!UiLoaded)
            {
                Debug.WriteLine("Reutnring from Update becaus ui not initialized");
                return;
            }

            try
            {
                Debug.WriteLine("Enteirng updateformattedtext " + PerformingUpdate);
                if (PerformingUpdate)
                {
                    Debug.WriteLine("Already performing update");
                    return;
                    throw new InvalidOperationException("Already performing update");
                }

                PerformingUpdate = true;
                RaiseEvent(new RoutedEventArgs(RenderStartEvent, this));
                // Geometries.Clear();

                // GeoTuples.Clear();

                // Make sure all UI is loaded

                var textStorePosition = 0;
                var linePosition = new Point(_xOffset, 0);

                // Create a DrawingGroup object for storing formatted text.

                _textDest.Children.Clear();

                // Format each line of text from the text store and draw it.
                TextLineBreak prev = null;
                LineInfo prevLine = null;
                CharacterCell prevCell = null;
                RegionInfo prevRegion = null;
                var line = 0;
                if (_nliens == 0) _nliens = 10;

                Debug.WriteLine("Calling inner update");
                var compilation = Compilation;

                var node0 = SyntaxNode;
                var tree = SyntaxTree;
                // _scrollViewer.VerticalScrollBarVisibility = ScrollBarVisibility.Disabled;
                var fontFamilyFamilyName = FontFamily.FamilyNames[XmlLanguage.GetLanguage("en-US")];
                Debug.WriteLine(fontFamilyFamilyName);
                Debug.WriteLine("OutputWidth " + OutputWidth);
                _rectangle.Width = OutputWidth + _rectangle.StrokeThickness * 2;
                var emSize = FontSize;
                var dispatcherOperation = SecondaryDispatcher.InvokeAsync(() => InnerUpdate(this, textStorePosition,
                    prev, prevLine, line, linePosition, prevCell,
                    prevRegion,
                    Formatter, OutputWidth, PixelsPerDip, emSize, tree, node0, compilation,
                    fontFamilyFamilyName, _channel.Writer));
                InnerUpdateDispatcherOperation = dispatcherOperation;
                var source = await dispatcherOperation.Task
                    .ContinueWith(
                        task =>
                        {
                            if (task.IsFaulted)
                            {
                                var xx1 = task.Exception.Flatten().ToString();
                                Debug.WriteLine(xx1);
                                Debug.WriteLine(task.Exception.ToString());
                            }

                            return task.Result;
                        }).ConfigureAwait(true);
                InnerUpdateDispatcherOperation = null;
                _scrollViewer.VerticalScrollBarVisibility = ScrollBarVisibility.Visible;
                CustomTextSource = source;
                Debug.WriteLine("Return from await inner update");
                ;
                // Persist the drawn text content.

                // _rectangle.Width = MaxX;
                Debug.WriteLine("Setting reactangle width to " + MaxX);
                // _rectangle.Height = _pos.Y;

                PerformingUpdate = false;
                InitialUpdate = false;
                RaiseEvent(new RoutedEventArgs(RenderCompleteEvent, this));
                // InsertionCharacter = LineInfos[0].Regions[0].Characters[0];
                // UpdateCaretPosition();
//                InvalidateVisual();
            }
            catch (Exception ex)
            {
                throw;
            }
            finally
            {
            }
        }

        public DispatcherOperation<CustomTextSource4> InnerUpdateDispatcherOperation
        {
            get { return _innerUpdateDispatcherOperation; }
            set
            {
                if (Equals(value, _innerUpdateDispatcherOperation)) return;
                _innerUpdateDispatcherOperation = value;
                OnPropertyChanged();
            }
        }

        public bool PerformingUpdate
        {
            get { return _performingUpdate; }
            set
            {
                if (value == _performingUpdate) return;
                Debug.WriteLine("Performing update set to " + value);
                _performingUpdate = value;
                OnPropertyChanged();
            }
        }

        public CustomTextSource4 CustomTextSource
        {
            get { return _customTextSource; }
            set
            {
                if (Equals(value, _customTextSource)) return;
                _customTextSource = value;
                OnPropertyChanged();
            }
        }

        /// <inheritdoc />
        protected override async void OnFilenameChanged(string oldValue, string newValue)
        {
            base.OnFilenameChanged(oldValue, newValue);
            if (newValue != null)
            {
                using (var sr = File.OpenText(newValue))
                {

                    var code = await sr.ReadToEndAsync();

                    SourceText = code;
                }
            }
        }

        private static CustomTextSource4 InnerUpdate(RoslynCodeControl roslynCodeControl, int textStorePosition,
            TextLineBreak prev, LineInfo prevLine, int line, Point linePosition,
            CharacterCell prevCell, RegionInfo prevRegion,
            TextFormatter textFormatter, double paragraphWidth, double pixelsPerDip,
            double emSize0, SyntaxTree tree, SyntaxNode node0, Compilation compilation, string faceName,
            ChannelWriter<UpdateInfo> channelWriter)
        {
            var s1 = new DispatcherSynchronizationContext(Dispatcher.CurrentDispatcher);
            if (s1 == null) throw new InvalidOperationException("no synchh context");
            var tf = roslynCodeControl.CreateTypeface(new FontFamily(faceName), FontStyles.Normal,
                FontStretches.Normal,
                FontWeights.Normal);

            var CurrentRendering1 = FontRendering.CreateInstance(emSize0,
                TextAlignment.Left,
                null,
                Brushes.Black,
                tf);
            var customTextSource4 =
                roslynCodeControl.CreateAndInitTextSource(pixelsPerDip, tf, tree, node0, compilation, s1, emSize0);
            var chars = new List<List<char>>();
            var startTime = DateTime.Now;
            var myGroup = new DrawingGroup();
            var myDc = myGroup.Open();
            var lineContext = new LineContext();
            var genericTextParagraphProperties =
                new GenericTextParagraphProperties(CurrentRendering1, pixelsPerDip);
            var runsInfos = new List<Tuple<TextRun, Rect>>();

            while (textStorePosition < customTextSource4.Length)
            {
                var runCount = customTextSource4.Runs.Count;
#if DEBUGTEXTSOURCE
                Debug.WriteLine("Runcount = " + runCount);
#endif
                using (var myTextLine = textFormatter.FormatLine(customTextSource4,
                    textStorePosition, paragraphWidth,
                    genericTextParagraphProperties,
                    prev))
                {
                    var nRuns = customTextSource4.Runs.Count - runCount;
#if DEBUGTEXTSOURCE
                    Debug.WriteLine("num runs for line is "  + nRuns);
#endif
                    var lineChars = new List<char>();
                    chars.Add(lineChars);
                    if (myTextLine.HasOverflowed) Debug.WriteLine("overflowed");

                    if (myTextLine.Width > paragraphWidth) Debug.WriteLine("overflowed2");
                    var lineInfo = new LineInfo {Offset = textStorePosition, Length = myTextLine.Length};
                    lineInfo.PrevLine = prevLine;
                    lineInfo.LineNumber = line;

                    if (prevLine != null) prevLine.NextLine = lineInfo;

                    prevLine = lineInfo;
                    lineInfo.Size = new Size(myTextLine.WidthIncludingTrailingWhitespace, myTextLine.Height);
                    lineInfo.Origin = new Point(linePosition.X, linePosition.Y);

                    var location = linePosition;
                    var group = 0;

                    var textRunSpans = myTextLine.GetTextRunSpans();
                    var spans = textRunSpans;
                    var cell = linePosition;
                    var cellColumn = 0;
                    var characterOffset = textStorePosition;
                    var regionOffset = textStorePosition;

                    var curPos = linePosition;
                    var positions = new List<Rect>();
                    var indexedGlyphRuns = myTextLine.GetIndexedGlyphRuns();
                    var textRuns = customTextSource4.Runs.GetRange(runCount, nRuns);
                    var enum1 = textRuns.GetEnumerator();
                    enum1.MoveNext();
                    foreach (var glyphRunC in indexedGlyphRuns)
                    {
                        var gl = glyphRunC.GlyphRun;
                        var bo = gl.BaselineOrigin;
                        var advanceSum = gl.AdvanceWidths.Sum();
                        var item = new Rect(curPos, new Size(advanceSum, myTextLine.Height));
                        runsInfos.Add(Tuple.Create(enum1.Current, item));
                        positions.Add(item);
                        curPos.X += advanceSum;
                        enum1.MoveNext();
                    }
#if DEBUGTEXTSOURCE
                    if (positions.Count != nRuns - 1)
                    {
                        Debug.WriteLine("number of line positions does not match number of runs");
                        var z = string.Join("",
                            indexedGlyphRuns.SelectMany(iz => iz.GlyphRun.Characters));
                        
                        foreach (var textRun in textRuns)
                        {
                            if (textRun is CustomTextCharacters c1)
                            {
                                var tt = c1.Text;
                            } else
                            {

                            }
                        }
                        Debug.WriteLine(z);
                    }
#endif
                    // FormattingHelper.HandleTextLine(null, ref lineContext, out var lineInfo2, null);
#if false
                    var eol = myTextLine.GetTextRunSpans().Select(xx => xx.Value).OfType<TextEndOfLine>();
                    if (eol.Any())
                    {
                        // dc.DrawRectangle(Brushes.Aqua, null,
                        // new Rect(linePosition.X + myTextLine.WidthIncludingTrailingWhitespace + 2,
                        // linePosition.Y + 2, 10, 10));
                    }
                    else
                    {
                        Debug.WriteLine("no end of line");
                        foreach (var textRunSpan in myTextLine.GetTextRunSpans())
                            Debug.WriteLine(textRunSpan.Value.ToString());
                    }

                    var lineRegions = new List<RegionInfo>();
                    lineInfo.Regions = lineRegions;
                    var lineString = "";
                    var xoffset = lineInfo.Origin.X;
                    var xoffsets = new List<double>();

                    var curOffset = linePosition;
                    foreach (var rect in myTextLine.GetIndexedGlyphRuns())
                    {
                        var rectGlyphRun = rect.GlyphRun;

                        if (rectGlyphRun != null)
                        {
                            var size = new Size(0, 0);
                            var cellBounds =
                                new List<CharacterCell>();
                            var emSize = rectGlyphRun.FontRenderingEmSize;


                            if (rectGlyphRun.Characters.Count > rectGlyphRun.GlyphIndices.Count)
                                Debug.WriteLine($"Character mismatch");

                            var xx = new RectangleGeometry(new Rect(curOffset,
                                new Size(rectGlyphRun.AdvanceWidths.Sum(),
                                    rectGlyphRun.GlyphTypeface.Height * rectGlyphRun.FontRenderingEmSize)));
                            curOffset.Y += myTextLine.Height;
                            var x = new CombinedGeometry();

                            for (var i = 0; i < rectGlyphRun.GlyphIndices.Count; i++)
                            {
                                var advanceWidth = rectGlyphRun.AdvanceWidths[i];

                                xoffsets.Add(xoffset);
                                xoffset += advanceWidth;
                                size.Width += advanceWidth;
                                var gi = rectGlyphRun.GlyphIndices[i];
                                var c = rectGlyphRun.Characters[i];
                                lineChars.Add(c);
                                lineString += c;
                                var advWidth = rectGlyphRun.GlyphTypeface.AdvanceWidths[gi];
                                var advHeight = rectGlyphRun.GlyphTypeface.AdvanceHeights[gi];

                                var s = new Size(advWidth * emSize,
                                    (advHeight
                                     + rectGlyphRun.GlyphTypeface.BottomSideBearings[gi])
                                    * emSize);

                                var topSide = rectGlyphRun.GlyphTypeface.TopSideBearings[gi];
                                var bounds = new Rect(new Point(cell.X, cell.Y + topSide), s);
                                if (!bounds.IsEmpty)
                                {
                                    // ReSharper disable once UnusedVariable
                                    var glyphTypefaceBaseline = rectGlyphRun.GlyphTypeface.Baseline;
                                    //Debug.WriteLine(glyphTypefaceBaseline.ToString());
                                    //bounds.Offset(cell.X, cell.Y + glyphTypefaceBaseline);
                                    // dc.DrawRectangle(Brushes.White, null,  bounds);
                                    // dc.DrawText(
                                    // new FormattedText(cellColumn.ToString(), CultureInfo.CurrentCulture,
                                    // FlowDirection.LeftToRight, new Typeface("Arial"), _emSize * .66, Brushes.Aqua,
                                    // new NumberSubstitution(), _pixelsPerDip), new Point(bounds.Left, bounds.Top));
                                }

                                var char0 = new CharacterCell(bounds, new Point(cellColumn, chars.Count - 1), c)
                                {
                                    PreviousCell = prevCell
                                };

                                if (prevCell != null)
                                    prevCell.NextCell = char0;
                                prevCell = char0;

                                cellBounds.Add(char0);
                                cell.Offset(rectGlyphRun.AdvanceWidths[i], 0);

                                cellColumn++;
                                characterOffset++;
                                //                                _textDest.Children.Add(new GeometryDrawing(null, new Pen(Brushes.DarkOrange, 2), new RectangleGeometry(bounds)));
                            }

                            //var bb = rect.GlyphRun.BuildGeometry().Bounds;

                            size.Height += myTextLine.Height;
                            var r = new Rect(location, size);
                            location.Offset(size.Width, 0);
//                            dc.DrawRectangle(null, new Pen(Brushes.Green, 1), r);
                            //rects.Add(r);
                            if (@group < spans.Count)
                            {
                                var textSpan = spans[@group];
                                var textSpanValue = textSpan.Value;
                                SyntaxNode node = null;
                                SyntaxToken? token = null;
                                SyntaxTrivia? trivia = null;
                                SyntaxToken? AttachedToken = null;
                                SyntaxNode attachedNode = null;

                                SyntaxNode structuredTrivia = null;
                                TriviaPosition? triviaPosition = null;
                                if (textSpanValue is SyntaxTokenTextCharacters stc)
                                {
                                    node = stc.SyntaxNode;
                                    token = stc.Token;
                                }
                                else
                                {
                                    if (textSpanValue is SyntaxTriviaTextCharacters stc2)
                                    {
                                        trivia = stc2.Trivia;
                                        AttachedToken = stc2.Token;
                                        attachedNode = stc2.SyntaxNode;
                                        structuredTrivia = stc2.StructuredTrivia;
                                        triviaPosition = stc2.TriviaPosition;
                                    }
                                }

                                var tuple = new RegionInfo(textSpanValue, r, cellBounds)
                                {
                                    Line = lineInfo,
                                    Offset = regionOffset,
                                    Length = textSpan.Length,
                                    SyntaxNode = node,
                                    AttachedToken = AttachedToken,
                                    AttachedNode = attachedNode,
                                    SyntaxToken = token,
                                    Trivia = trivia,
                                    TriviaPosition = triviaPosition,
                                    PrevRegion = prevRegion,
                                    StructuredTrivia = structuredTrivia
                                };
                                foreach (var ch in tuple.Characters) ch.Region = tuple;
                                lineRegions.Add(tuple);

                                //roslynCodeControl.GeoTuples.Add(Tuple.Create(xx, tuple));

                                if (prevRegion != null) prevRegion.NextRegion = tuple;
                                prevRegion = tuple;
                                // Infos.Add(tuple);
                            }

                            @group++;
                            regionOffset = characterOffset;
                        }

                        lineInfo.Text = lineString;
                        lineInfo.Regions = lineRegions;
                        //                        Debug.WriteLine(rect.ToString());
                        //dc.DrawRectangle(null, new Pen(Brushes.Green, 1), r1);
                    }


                    //Debug.WriteLine(line.ToString() + ddBounds.ToString());
                    //dc.DrawRectangle(null, new Pen(Brushes.Red, 1), ddBounds);
#endif
                    // Draw the formatted text into the drawing context.
                    var p = new Point(linePosition.X + myTextLine.WidthIncludingTrailingWhitespace, linePosition.Y);
                    var w = myTextLine.Width;

                    myTextLine.Draw(myDc, linePosition, InvertAxes.None);
                    linePosition.Y += myTextLine.Height;
                    if (false)
                        roslynCodeControl.Dispatcher.Invoke(() =>
                        {
                            if (w >= roslynCodeControl.MaxX) roslynCodeControl.MaxX = w;
                            var dc = roslynCodeControl._textDest.Append();
                            myTextLine.Draw(dc, linePosition, InvertAxes.None);
                            dc.Close();

                            // roslynCodeControl.Translate.X = -1 * roslynCodeControl._textDest.Bounds.Left;

                            var rectangleWidth = roslynCodeControl.MaxX + roslynCodeControl._xOffset;
                            roslynCodeControl._rectangle.Width = rectangleWidth;
                            var rectangleHeight =
                                Math.Min(roslynCodeControl._pos.Y, roslynCodeControl.ActualHeight);
                            roslynCodeControl._rectangle.Height = rectangleHeight;
                            roslynCodeControl._myDrawingBrush.Viewbox =
                                new Rect(0, 0, rectangleWidth, rectangleHeight);
                            roslynCodeControl._myDrawingBrush.ViewboxUnits = BrushMappingMode.Absolute;
                            roslynCodeControl.LineInfos.Add(lineInfo);
                        });
                    // ReSharper disable once UnusedVariable

                    // var textLineBreak = myTextLine.GetTextLineBreak();
                    // if (textLineBreak != null)
                    // Debug.WriteLine(textLineBreak.ToString());
                    line++;

                    prev = null;
                    // if (prev != null) Debug.WriteLine("Line break!");

                    // Update the index position in the text store.
                    textStorePosition += myTextLine.Length;
                    // Update the line position coordinate for the displayed line.
                }

#if false
                roslynCodeControl.Dispatcher.Invoke(() => { roslynCodeControl._pos = linePosition; });
#endif

                if (line > 0 && line % 100 == 0)
                {
                    myDc.Close();

                    RenderTargetBitmap SaveImage(Drawing d)
                    {
                        var b = new DrawingBrush(d);
                        var v = new DrawingVisual();
                        var dc = v.RenderOpen();
                        var rect1 = new Rect(new Point(0, 0), d.Bounds.Size);

                        dc.DrawRectangle(b, null, rect1);
                        dc.Close();
                        var width = (int) rect1.Width;
                        var height = (int) rect1.Height;
                        var rtb = new RenderTargetBitmap(width, height, 96,
                            96,
                            PixelFormats.Pbgra32);
                        rtb.Render(v);
                        return rtb;
                    }


                    var out1 = SaveImage(myGroup);
                    out1.Freeze();
                    myDc = myGroup.Open();
                    var rect = myGroup.Bounds; //new Rect(0, 0, myGroup.Bounds.Width, myGroup.Bounds.Height);
                    var w = myGroup.Bounds.Width;
                    // Debug.WriteLine("width = " + w);
                    var y = myGroup.Bounds.Bottom;
                    // Debug.WriteLine("bottom = " + (int) y);
                    var curUi = new UpdateInfo() {ImageSource = out1, Rect = rect};
                    channelWriter.WriteAsync(curUi);
#if false
                    roslynCodeControl.Dispatcher.Invoke(() =>
                    {
                        var dc = roslynCodeControl._textDest.Append();
                        dc.DrawImage(out1, rect);
                        dc.Close();


                        if (w >= roslynCodeControl.MaxX) roslynCodeControl.MaxX = w;

                        var rectangleWidth = roslynCodeControl.MaxX + roslynCodeControl._xOffset;
                        // roslynCodeControl._rectangle.Width = rectangleWidth;

                        roslynCodeControl._rectangle.Height = y;
                        roslynCodeControl._myDrawingBrush.Viewbox =
                            new Rect(0, 0, paragraphWidth, y);
                        roslynCodeControl._myDrawingBrush.ViewboxUnits = BrushMappingMode.Absolute;
                    });
#endif
                    var span = DateTime.Now - startTime;
                    // Debug.WriteLine("Process line took " + span);
                    startTime = DateTime.Now;
                }
            }


            if (line % 100 != 0)
            {
                myDc.Close();

                RenderTargetBitmap SaveImage(Drawing d)
                {
                    var b = new DrawingBrush(d);
                    var v = new DrawingVisual();
                    var dc = v.RenderOpen();
                    var rect1 = new Rect(new Point(0, 0), d.Bounds.Size);

                    dc.DrawRectangle(b, null, rect1);
                    dc.Close();
                    var width = (int) rect1.Width;
                    var height = (int) rect1.Height;
                    var rtb = new RenderTargetBitmap(width, height, 96,
                        96,
                        PixelFormats.Pbgra32);
                    rtb.Render(v);
                    return rtb;
                }


                var out1 = SaveImage(myGroup);
                out1.Freeze();
                myDc = myGroup.Open();
                var rect = myGroup.Bounds; //new Rect(0, 0, myGroup.Bounds.Width, myGroup.Bounds.Height);
                var w = myGroup.Bounds.Width;
                // Debug.WriteLine("width = " + w);
                var y = myGroup.Bounds.Bottom;
                var curUi = new UpdateInfo() {ImageSource = out1, Rect = rect};
                channelWriter.WriteAsync(curUi);

                var span = DateTime.Now - startTime;
                // Debug.WriteLine("Process line took " + span);
                startTime = DateTime.Now;
            }

            myDc.Close();

            customTextSource4.RunInfos = runsInfos;
            return customTextSource4;
        }

        public GeometryCollection Geometries { get; set; } = new GeometryCollection(200);

        private void UpdateCaretPosition()
        {
            var insertionPoint = InsertionPoint;
            var l0 = LineInfos.FirstOrDefault(l => l.Offset + l.Length >= insertionPoint);
            if (l0 != null)
            {
                InsertionLine = l0;
                _textCaret.SetValue(Canvas.TopProperty, l0.Origin.Y);
                var rr = l0.Regions.FirstOrDefault(r => r.Offset + r.Length > insertionPoint);
                InsertionRegion = rr;
                if (rr != null)
                {
                    var ch = rr.Characters[insertionPoint - rr.Offset];
                    InsertionCharacter = ch;
                    var x = ch.Bounds.Right - ch.Bounds.Width / 2;
                    _textCaret.SetValue(Canvas.LeftProperty, x);
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public double OutputWidth { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public ObservableCollection<LineInfo> LineInfos { get; } = new ObservableCollection<LineInfo>();

        // ReSharper disable once NotAccessedField.Local
        private Border _border;

        // ReSharper disable once NotAccessedField.Local
        private Grid _grid;
        private Rectangle _rectangle;
        private ScrollViewer _scrollViewer;
        private GenericTextRunProperties _baseProps;

        /// <summary>
        /// 
        /// </summary>
        public Typeface Typeface { get; protected set; }

        private FontFamily _fontFamily;
        private readonly FontStyle _fontStyle = FontStyles.Normal;
        private readonly FontWeight _fontWeight = FontWeights.Normal;
        private readonly FontStretch _fontStretch = FontStretches.Normal;

        
        // ReSharper disable once NotAccessedField.Local
        private int _startColumn;

        // ReSharper disable once NotAccessedField.Local
        private int _startRow;
        private int _startOffset;
        private DrawingGroup _selectionGeometry;
        private Rectangle _rect2;
        private DrawingGroup _dg2;
        private Grid _innerGrid;
        private TextCaret _textCaret;
        private Canvas _canvas;
        private FontRendering _currentRendering;
        private CustomTextSource4 _store;

        private int _selectionEnd;
        private SyntaxNode _startNode;
        private SyntaxNode _endNode;
        private Rectangle geometryRectangle = new Rectangle();
        private SourceText _text;
        private int _nliens;
        private ComboBox _fontSizeCombo;
        private ComboBox _fontCombo;
        private ObjectAnimationUsingKeyFrames _x1;
        private CustomTextSource4 _customTextSource;
        private Dispatcher _secondaryDispatcher;
        private double _xOffset = 0.0;
        private ISymbol _enclosingSymbol;
        private DispatcherOperation<Task> _updateOperation;
        private DocumentPaginator _documentPaginator1 = null;
        private bool _performingUpdate;
        private DispatcherOperation<CustomTextSource4> _innerUpdateDispatcherOperation;
        private Task _updateFormattedTestTask;
        private ChannelReader<UpdateInfo> _reader;
        private Channel<UpdateInfo> _channel;

        /// <inheritdoc />
        protected override void OnMouseMove(MouseEventArgs e)
        {
            DrawingContext dc = null;
            try
            {
                //dc = _dg2.Open();
                // if (SelectionEnabled && e.LeftButton == MouseButtonState.Pressed)
                // {
                var point = e.GetPosition(_rectangle);
                if (CustomTextSource?.RunInfos != null)
                {
                    var runInfo = CustomTextSource.RunInfos.Where(zz1 => zz1.Item2.Contains(point));
                    if (runInfo.Any())
                    {
                        Debug.WriteLine(runInfo.Count().ToString());
                        var first = runInfo.First();
                        Debug.WriteLine(first.Item2.ToString());
                        Debug.WriteLine(first.Item1.ToString() ?? "");
                        if (first.Item1 is CustomTextCharacters c0) Debug.WriteLine(c0.Text);
                        HoverRegionInfo = new RegionInfo(first.Item1, first.Item2, new List<CharacterCell>());
                    }
                }

                return;
                var q = LineInfos.SkipWhile(z => z.Origin.Y < point.Y);
                if (q.Any())
                {
                    var line = q.First();
                    // Debug.WriteLine(line.LineNumber.ToString());
                    if (line.Regions != null)
                    {
                        var qq = line.Regions.SkipWhile(zz0 => !zz0.BoundingRect.Contains(point));
                        if (qq.Any())
                        {
                            var region = qq.First();
                            Debug.WriteLine(region.SyntaxToken?.ToString());
                        }
                    }
                }

                var zz = LineInfos.Where(z => z.Regions != null).SelectMany(z => z.Regions)
                    .Where(x => x.BoundingRect.Contains(point)).ToList();
                if (zz.Count > 1)
                    Debug.WriteLine("Multiple regions matched");
                //    throw new AppInvalidOperationException();

                // Retrieve the coordinate of the mouse position.


                // Perform the hit test against a given portion of the visual object tree.
                // var drawingVisual = new DrawingVisual();
                // var drawingVisualDrawing = new DrawingGroup();
                // var dc = drawingVisual.RenderOpen();

                // foreach (var g in GeoTuples)
                // {
                // dc.DrawGeometry(Brushes.Black, null, g);
                // }

                // foreach (var g in GeoTuples)
                // if (g.Item1.Rect.Contains(point))
                // Debug.WriteLine(g.Item2.SyntaxNode?.Kind().ToString() ?? "");
                // Debug.WriteLine(((RectangleGeometry)g).Rect);

                //

                // dc.Close();


                // var result = VisualTreeHelper.HitTest(drawingVisual, point);

                // if (result != null)
                // {
                // Perform action on hit visual object.
                // }

                if (!zz.Any())
                {
                    HoverColumn = 0;
                    HoverSyntaxNode = null;
                    HoverOffset = 0;
                    HoverRegionInfo = null;
                    HoverRow = 0;
                    HoverSymbol = null;
                    HoverToken = null;
                }

                foreach (var tuple in zz)
                {
                    HoverRegionInfo = tuple;
                    // var dc1 = RegionDG.Open();
                    // dc1.DrawRectangle(null, new Pen(Brushes.Red, 2), tuple.BoundingRect);
                    // dc1.Close();

                    // _dg2.Children.Add(new GeometryDrawing(null, 
                    // if (tuple.Trivia.HasValue) Debug.WriteLine(tuple
                    // ~.ToString());

                    if (tuple.SyntaxNode != HoverSyntaxNode)
                    {
                        if (ToolTip is ToolTip tt) tt.IsOpen = false;
                        HoverSyntaxNode = tuple.SyntaxNode;
                        if (tuple.SyntaxNode != null)
                        {
                            ISymbol sym = null;
                            IOperation operation = null;
                            if (Model != null)
                                try
                                {
                                    sym = Model?.GetDeclaredSymbol(tuple.SyntaxNode);
                                    operation = Model.GetOperation(tuple.SyntaxNode);
                                    // var zzz = tuple.SyntaxNode.AncestorsAndSelf().OfType<ForEachStatementSyntax>()
                                    // .FirstOrDefault();
                                    // if (zzz != null)
                                    // {
                                    // var info = Model.GetForEachStatementInfo(zzz);
                                    // Debug.WriteLine(info.ElementType?.ToDisplayString());
                                    // }

                                    // switch ((CSharpSyntaxNode) tuple.SyntaxNode)
                                    // {
                                    // case AssignmentExpressionSyntax assignmentExpressionSyntax:
                                    // break;
                                    // case ForEachStatementSyntax forEachStatementSyntax:
                                    // var info = Model.GetForEachStatementInfo(forEachStatementSyntax);
                                    // Debug.WriteLine(info.ElementType.ToDisplayString());
                                    // break;
                                    // case ForEachVariableStatementSyntax forEachVariableStatementSyntax:
                                    // break;
                                    // case MethodDeclarationSyntax methodDeclarationSyntax:

                                    // break;
                                    // case TryStatementSyntax tryStatementSyntax:
                                    // break;
                                    // case StatementSyntax statementSyntax:
                                    // break;
                                    // default:
                                    // break;
                                    // }
                                }
                                catch
                                {
                                    // ignored
                                }

                            if (sym != null)
                            {
                                HoverSymbol = sym;
                                Debug.WriteLine(sym.Kind.ToString());
                            }

                            var node = tuple.SyntaxNode;
                            var nodes = new Stack<SyntaxNodeDepth>();
                            var depth = 0;
                            while (node != null)
                            {
                                node = node.Parent;
                                depth++;
                            }

                            depth--;
                            node = tuple.SyntaxNode;
                            while (node != null)
                            {
                                nodes.Push(new SyntaxNodeDepth {SyntaxNode = node, Depth = depth});
                                node = node.Parent;
                                depth--;
                            }


                            var content = new CodeToolTipContent()
                                {Symbol = sym, SyntaxNode = tuple.SyntaxNode, Nodes = nodes, Operation = operation};
                            var template =
                                TryFindResource(new DataTemplateKey(typeof(CodeToolTipContent))) as DataTemplate;
                            var toolTip = new ToolTip {Content = content, ContentTemplate = template};
                            ToolTip = toolTip;
                            toolTip.IsOpen = true;
                        }
                    }

                    if (tuple.SyntaxNode != null)
                    {
                    }

                    HoverToken = tuple.SyntaxToken;

                    var cellIndex = tuple.Characters.FindIndex(zx => zx.Bounds.Contains(point));
                    if (cellIndex != -1)
                    {
                        var cell = tuple.Characters[cellIndex];

                        var first = cell;
                        var item2 = first.Point;

                        var item2Y = (int) item2.Y;
                        // if (item2Y >= _chars.Count)
                        // {
                        // Debug.WriteLine("out of bounds");
                        // }
                        // else
                        // {
                        // var chars = _chars[item2Y];
                        // Debug.WriteLine("y is " + item2Y, DebugCategory.MouseEvents);
                        // var item2X = (int) item2.X;
                        // if (item2X >= chars.Count)
                        // {
                        //Debug.WriteLine("out of bounds");
                        // }
                        // else
                        // {
                        // var ch = chars[item2X];
                        // Debug.WriteLine("Cell is " + item2 + " " + ch, DebugCategory.MouseEvents);
                        var newOffset = tuple.Offset + cellIndex;
                        HoverOffset = newOffset;
                        HoverColumn = (int) item2.X;
                        HoverRow = (int) item2.Y;
                        if (SelectionEnabled && IsSelecting)
                        {
                            if (_selectionGeometry != null) _textDest.Children.Remove(_selectionGeometry);
                            Debug.WriteLine("Calculating selection");

                            var group = new DrawingGroup();

                            int begin;
                            int end;
                            if (_startOffset < newOffset)
                            {
                                begin = _startOffset;
                                end = newOffset;
                            }
                            else
                            {
                                begin = newOffset;
                                end = _startOffset;
                            }

                            var green = new SolidColorBrush(Colors.Green) {Opacity = .2};
                            var blue = new SolidColorBrush(Colors.Blue) {Opacity = .2};
                            var red = new SolidColorBrush(Colors.Red) {Opacity = .2};
                            foreach (var regionInfo in LineInfos.SelectMany(z => z.Regions).Where(info =>
                                info.Offset <= begin && info.Offset + info.Length > begin ||
                                info.Offset >= begin && info.Offset + info.Length <= end))
                            {
                                Debug.WriteLine(
                                    $"Region offset {regionInfo.Offset} : Length {regionInfo.Length}");
                                    
                                if (regionInfo.Offset <= begin)
                                {
                                    var takeNum = begin - regionInfo.Offset;
                                    Debug.WriteLine("Taking " + takeNum);
                                    foreach (var tuple1 in regionInfo.Characters.Take(takeNum))
                                    {
                                        Debug.WriteLine("Adding " + tuple1);
                                        @group.Children.Add(new GeometryDrawing(red, null,
                                            new RectangleGeometry(tuple1.Bounds)));
                                    }

                                    continue;
                                }

                                if (regionInfo.Offset + regionInfo.Length > end)
                                {
                                    foreach (var tuple1 in regionInfo.Characters.Take(end - regionInfo.Offset))
                                        @group.Children.Add(new GeometryDrawing(blue, null,
                                            new RectangleGeometry(tuple1.Bounds)));

                                    continue;
                                }

                                var geo = new RectangleGeometry(regionInfo.BoundingRect);
                                @group.Children.Add(new GeometryDrawing(green, null, geo));
                            }


                            _selectionGeometry = @group;
                            // _dg2.Children.Add(_selectionGeometry);
                            // _myDrawingBrush.Drawing = _textDest;
                            _selectionEnd = newOffset;
                            // InvalidateVisual();
                        }
                    }

                    var textRunProperties = tuple.TextRun.Properties;
                    if (!(textRunProperties is GenericTextRunProperties)) continue;
                    if (_rect != tuple.BoundingRect)
                    {
                        _rect = tuple.BoundingRect;
                        // if (_geometryDrawing != null) _textDest.Children.Remove(_geometryDrawing);


                        var solidColorBrush = new SolidColorBrush(Colors.CadetBlue) {Opacity = .6};


                        _geometryDrawing =
                            new GeometryDrawing(solidColorBrush, null, new RectangleGeometry(tuple.BoundingRect));

                        // _dg2.Children.Add(_geometryDrawing);
                        // InvalidateVisual();
                    }

                    //Debug.WriteLine(pp.Text);
                }

                if (SelectionEnabled && e.LeftButton == MouseButtonState.Pressed)
                    if (!IsSelecting)
                    {
                        var xy = e.GetPosition(_scrollViewer);
                        if (xy.X < _scrollViewer.ViewportWidth && xy.X >= 0 && xy.Y >= 0 &&
                            xy.Y <= _scrollViewer.ViewportHeight)
                        {
                            _startOffset = HoverOffset;
                            _startRow = HoverRow;
                            _startColumn = HoverColumn;
                            _startNode = HoverSyntaxNode;


                            IsSelecting = true;
                            e.Handled = true;
                            _rectangle.CaptureMouse();
                        }
                    }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
            }
            finally
            {
                dc?.Close();
            }
        }

        private bool z(SyntaxNode arg)
        {
            return arg.Kind() == SyntaxKind.ForEachStatement;
        }

        /// <inheritdoc />
        protected override void OnMouseUp(MouseButtonEventArgs e)
        {
            if (IsSelecting)
            {
                IsSelecting = false;
                _endNode = HoverSyntaxNode;
                Debug.WriteLine($"{_startOffset} {_selectionEnd}");
                if (_startNode != null)
                    if (_endNode != null)
                    {
                        var st1 = _startNode.AncestorsAndSelf().OfType<StatementSyntax>().FirstOrDefault();
                        var st2 = _endNode.AncestorsAndSelf().OfType<StatementSyntax>().FirstOrDefault();
                        if (st1 != null)
                            if (st2 != null)
                                if (Model != null)
                                {
                                    var r = Model.AnalyzeDataFlow(st1, st2);
                                    if (r != null)
                                        return;
                                    Debug.WriteLine(r != null && r.Succeeded);
                                }
                    }

                _rectangle.ReleaseMouseCapture();
            }
        }

        /// <inheritdoc />
        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            InsertionPoint = HoverOffset;
        }

        /// <inheritdoc />
        protected override void OnPreviewMouseDown(MouseButtonEventArgs e)
        {
        }

        private static void OnNodeUpdated(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var ss = (RoslynCodeControl)d;
            ss.OnNodeUpdated();
        }

        /// <summary>
        /// 
        /// </summary>
        protected virtual async void OnNodeUpdated()
        {
            if (!(ChangingText || UpdatingSourceText))
            {
                Debug.WriteLine("SyntaxNode updated");
                LineInfos.Clear();
                MaxX = 0;
                MaxY = 0;
                _pos = new Point(_xOffset, 0);
                if (_scrollViewer != null) _scrollViewer.ScrollToTop();
                if (SecondaryDispatcher != null)
                    await UpdateTextSource();

                //UpdateFormattedText();
            }
        }


        public List<Tuple<RectangleGeometry, RegionInfo>> GeoTuples { get; set; } =
            new List<Tuple<RectangleGeometry, RegionInfo>>();

        /// <inheritdoc />
        public void PrepareDrawLines(LineContext lineContext, bool clear)
        {
        }

        /// <inheritdoc />
        public void PrepareDrawLine(LineContext lineContext)
        {
        }

        /// <inheritdoc />
        public void DrawLine(LineContext lineContext)
        {
        }

        /// <inheritdoc />
        public void EndDrawLines(LineContext lineContext)
        {
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        // /// <inheritdoc />
        // public DocumentPaginator DocumentPaginator
        // {
        //     get
        //     {
        //         if (_documentPaginator1 == null)
        //         {
        //             var target = new CodePaginator(this);
        //             var i = ProxyUtilsBase.CreateInterceptor(s => Debug.WriteLine(s));
        //             var pr = ProxyGeneratorHelper.ProxyGenerator.CreateClassProxyWithTarget<CodePaginator>(target, i);
        //             _documentPaginator1 = pr;
        //         }
        //
        //         return _documentPaginator1;
        //     }
        // }

        public Rectangle Rectangle1
        {
            get { return _rectangle; }
            set { _rectangle = value; }
        }

        public DispatcherOperation<Task> UpdateOperation
        {
            get { return _updateOperation; }
            set
            {
                if (Equals(value, _updateOperation)) return;
                _updateOperation = value;
                Debug.WriteLine("Setting update operation task");
            }
        }
    }

    public class UpdateInfo
    {
        public BitmapSource ImageSource { get; set; }
        public Rect Rect { get; set; }
    }
}