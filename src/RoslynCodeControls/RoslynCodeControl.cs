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

// ReSharper disable ConvertToUsingDeclaration

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

        public static readonly DependencyProperty InsertionCharInfoProperty = DependencyProperty.Register(
            "InsertionCharInfo", typeof(CharInfo), typeof(RoslynCodeControl), new PropertyMetadata(default(CharInfo)));

        public CharInfo InsertionCharInfo
        {
            get { return (CharInfo) GetValue(InsertionCharInfoProperty); }
            set { SetValue(InsertionCharInfoProperty, value); }
        }

        public static readonly DependencyProperty InsertionPointProperty = DependencyProperty.Register(
            "InsertionPoint", typeof(int), typeof(RoslynCodeControl),
            new PropertyMetadata(default(int), OnInsertionPointChanged, CoerceInsertionPoint));

        private static object CoerceInsertionPoint(DependencyObject d, object basevalue)
        {
            var p = (int) basevalue;
            var r = (RoslynCodeControl) d;
            if (r.CustomTextSource.Length - 2 < p) return r.CustomTextSource.Length - 2;

            return p;
        }

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
            if (!_updatingCaret)
                UpdateCaretPosition(oldValue, newValue);
            try
            {
                var enclosingsymbol = Model?.GetEnclosingSymbol(newValue);
                EnclosingSymbol = enclosingsymbol;

                if (EnclosingSymbol != null)
                    Debug.WriteLine(EnclosingSymbol.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat));
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
        /// <param name="synchContext"></param>
        /// <param name="fontSize"></param>
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
        public List<CompilationError> Errors { get; } = new List<CompilationError>();

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

        static RoslynCodeControl()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(RoslynCodeControl),
                new FrameworkPropertyMetadata(typeof(RoslynCodeControl)));
            SyntaxTreeProperty.OverrideMetadata(typeof(RoslynCodeControl),
                new FrameworkPropertyMetadata(default(SyntaxTree), FrameworkPropertyMetadataOptions.None,
                    OnSyntaxTreeChanged_));
            SyntaxNodeProperty.OverrideMetadata(typeof(RoslynCodeControl),
                new PropertyMetadata(default(SyntaxNode), OnNodeUpdated));
        }

        private static void OnSyntaxTreeChanged_(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var ss = (RoslynCodeControl) d;
            ss.OnSyntaxTreeUpdated((SyntaxTree) e.NewValue);
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
            _x1 = new ObjectAnimationUsingKeyFrames
            {
                RepeatBehavior = RepeatBehavior.Forever,
                Duration = new Duration(TimeSpan.FromSeconds(1))
            };
            Debug.WriteLine(_x1.Duration.ToString());

            var c = new ObjectKeyFrameCollection
            {
                new DiscreteObjectKeyFrame(Visibility.Visible),
                new DiscreteObjectKeyFrame(Visibility.Hidden, KeyTime.FromPercent(.6)),
                new DiscreteObjectKeyFrame(Visibility.Visible, KeyTime.FromPercent(.4))
            };
            _x1.KeyFrames = c;

            CSharpCompilationOptions = new CSharpCompilationOptions(default(OutputKind));
            PixelsPerDip = VisualTreeHelper.GetDpi(this).PixelsPerDip;
            // CommandBindings.Add(new CommandBinding(WpfAppCommands.Compile, CompileExecuted));
            CommandBindings.Add(new CommandBinding(EditingCommands.EnterLineBreak, OnEnterLineBreak,
                CanEnterLineBreak));
            CommandBindings.Add(new CommandBinding(EditingCommands.Backspace, OnBackspace));
            CommandBindings.Add(new CommandBinding(EditingCommands.MoveRightByCharacter, OnMoveRightByCharacter));
            CommandBindings.Add(new CommandBinding(EditingCommands.MoveLeftByCharacter, OnMoveLeftByCharacter));


            InputBindings.Add(new KeyBinding(EditingCommands.EnterLineBreak, Key.Enter, ModifierKeys.None));
            InputBindings.Add(new KeyBinding(EditingCommands.Backspace, Key.Back, ModifierKeys.None));
            InputBindings.Add(new KeyBinding(EditingCommands.MoveRightByCharacter, Key.Right, ModifierKeys.None));
            InputBindings.Add(new KeyBinding(EditingCommands.MoveLeftByCharacter, Key.Left, ModifierKeys.None));
        }

        private void OnMoveLeftByCharacter(object sender, ExecutedRoutedEventArgs e)
        {
            if (InsertionPoint > 0) InsertionPoint--;
        }

        private void OnMoveRightByCharacter(object sender, ExecutedRoutedEventArgs e)
        {
            InsertionPoint++;
        }

        private async void OnBackspace(object sender, ExecutedRoutedEventArgs e)
        {
            if (_handlingInput)
                return;
            _handlingInput = true;
            var b = await DoInput(new InputRequest(InputRequestKind.Backspace));
            if (!b)
                Debug.WriteLine("Backspace failed");
            _handlingInput = false;
        }

        private void ContinuationFunction(Task<UpdateInfo> z)
        {
            var ui = z.Result;
            CharInfos.AddRange(ui.CharInfos);
            var dg = ui.DrawingGroup;
            var dg2 = new DrawingGroup();
            foreach (var dgChild in dg.Children)
                //dg.Children.Remove(dgChild);
                dg2.Children.Add(dgChild);
            _textDest.Children.Add(dg2);
            var uiRect = dg2.Bounds;
            var maxY = Math.Max(MaxY, uiRect.Bottom);
            MaxY = maxY;
            var maxX = Math.Max(MaxX, uiRect.Right);
            MaxX = maxX;
            Rectangle.Height = maxY;
            Rectangle.Width = maxX;
            _myDrawingBrush.Viewbox = new Rect(0, 0, maxX, maxY);
            _myDrawingBrush.ViewboxUnits = BrushMappingMode.Absolute;
            _reader.ReadAsync().AsTask().ContinueWith(ContinuationFunction, CancellationToken.None,
                TaskContinuationOptions.None, TaskScheduler.FromCurrentSynchronizationContext());
        }

        public List<CharInfo> CharInfos { get; set; } = new List<CharInfo>();

        // private void CompileExecuted(object sender, ExecutedRoutedEventArgs e)
        // {
        //     if (Document != null)
        //         Document.Project.GetCompilationAsync().ContinueWith(task =>
        //         {
        //             Dispatcher.InvokeAsync(() => Compilation = task.Result);
        //         });
        // }

        private async void OnEnterLineBreak(object sender, ExecutedRoutedEventArgs e)
        {
            if (_handlingInput)
                return;
            _handlingInput = true;
            var b = await DoInput(new InputRequest(InputRequestKind.NewLine));
            if (!b)
                Debug.WriteLine("Newline failed");
            _handlingInput = false;
        }

        private void CanEnterLineBreak(object sender, CanExecuteRoutedEventArgs e)
        {
            if (_handlingInput) return;
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
                throw new InvalidOperationException();

            if (SyntaxNode == null || SyntaxTree == null) return;
            if (ReferenceEquals(SyntaxNode.SyntaxTree, SyntaxTree) == false)
                throw new InvalidOperationException("SyntaxNode is not within syntax tree");

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

        /// <inheritdoc />
        public override void OnApplyTemplate()
        {
            _scrollViewer = (ScrollViewer) GetTemplateChild("ScrollViewer");
            if (_scrollViewer != null) OutputWidth = _scrollViewer.ActualWidth;
            // if(OutputWidth == 0)
            // {
            // throw new InvalidOperationException();
            // }

            Debug.WriteLine(OutputWidth.ToString());

            Rectangle = (Rectangle) GetTemplateChild("Rectangle");
            var dpd = DependencyPropertyDescriptor.FromProperty(TextElement.FontSizeProperty, typeof(Rectangle));
            var dpd2 = DependencyPropertyDescriptor.FromProperty(TextElement.FontFamilyProperty, typeof(Rectangle));
            Translate = (TranslateTransform) GetTemplateChild("TranslateTransform");

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
        }

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
            StaticSecondaryDispatcher = d;
            // });
            Dispatcher.Run();
        }

        public static Dispatcher StaticSecondaryDispatcher { get; set; }

        public Dispatcher SecondaryDispatcher
        {
            get { return StaticSecondaryDispatcher; }
        }

        /// <summary>
        /// 
        /// </summary>
        public LineInfo InsertionLine { get; set; }


        /// <summary>
        /// 
        /// </summary>
        public RegionInfo InsertionRegion { get; set; }

        /// <inheritdoc />
        protected override async void OnPreviewTextInput(TextCompositionEventArgs e)
        {
            base.OnPreviewTextInput(e);
            if (_handlingInput)
                return;
            var eText = e.Text;
            e.Handled = true;
            try
            {
                _handlingInput = true;
                await DoInput(new InputRequest(InputRequestKind.TextInput, eText));
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
            }
            finally
            {
                _handlingInput = false;
            }
        }

        public async Task<bool> DoInput(InputRequest inputRequest)
        {
            var text = inputRequest.Text;
            try
            {
                if (CustomTextSource == null) await UpdateTextSource();

                Debug.WriteLine(text);

                var insertionPoint = InsertionPoint;
                string code;
                if (inputRequest.Kind != InputRequestKind.Backspace)
                {
                    var prev = SourceText.Substring(0, insertionPoint);
                    var next = SourceText.Substring(insertionPoint);
                    code = prev + text + next;
                }
                else
                {
                    code = SourceText;
                }

                await DoUpdateText(insertionPoint, code, inputRequest);
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
                return false;
            }
        }

        private async Task DoUpdateText(int insertionPoint, string code, InputRequest inputRequest)
        {
            var text = inputRequest.Text;
            var insertionLineOffset = InsertionLine?.Offset ?? 0;
            var originY = InsertionLine?.Origin.Y ?? 0;
            var originX = InsertionLine?.Origin.X ?? 0;
            var insertionLineLineNumber = InsertionLine?.LineNumber ?? 0;
            var insertionLine = InsertionLine;

            var d = new DrawingGroup();
            var drawingContext = d.Open();
            var typefaceName = FontFamily.FamilyNames[XmlLanguage.GetLanguage("en-US")];
            ;
            var inn = new InClassName(this, insertionLineLineNumber, insertionLineOffset, originY, originX,
                insertionLine, Formatter, OutputWidth, null, PixelsPerDip, CustomTextSource, MaxY, MaxX,
                d, drawingContext, FontSize, typefaceName);

            var lineInfo = await SecondaryDispatcher.InvokeAsync(
                new Func<LineInfo>(() => Callback(inn, insertionPoint, inputRequest)),
                DispatcherPriority.Send, CancellationToken.None);
            var in2 = new In2(this, insertionPoint, code, inputRequest, text, inn, lineInfo);
            await Dispatcher.Invoke(() => Callback2(in2));
        }

        private static async Task Callback2(In2 in2)
        {
            var roslynCodeControl = in2.RoslynCodeControl;
            var inputRequest = in2.InputRequest;
            var lineInfo = in2.LineInfo;
            var insertionPoint = in2.InsertionPoint;
            var text = in2.Text;
            var inn = in2.In1;
            var code = in2.Code;
            if (inputRequest.Kind == InputRequestKind.Backspace)
                roslynCodeControl.InsertionPoint--;
            else
                roslynCodeControl.InsertionPoint = insertionPoint + (text?.Length ?? 0);

            if (lineInfo == null) throw new InvalidOperationException();

            if (roslynCodeControl.InsertionPoint == lineInfo.Offset + lineInfo.Length)
            {
                var newLineInfo = new LineInfo()
                {
                    LineNumber = lineInfo.LineNumber + 1,
                    Offset = roslynCodeControl.InsertionPoint,
                    Origin = new Point(roslynCodeControl._xOffset,
                        roslynCodeControl.InsertionLine.Origin.Y + roslynCodeControl.InsertionLine.Height),
                    PrevLine = roslynCodeControl.InsertionLine
                };
                roslynCodeControl.InsertionLine = newLineInfo;

                var drawingGroup = new DrawingGroup();
                var dc = drawingGroup.Open();
                var inn2 = new InClassName(roslynCodeControl, newLineInfo.LineNumber, newLineInfo.Offset,
                    newLineInfo.Origin.Y, newLineInfo.Origin.X, newLineInfo, Formatter, inn.ParagraphWidth,
                    inn.CurrentRendering, inn.PixelsPerDip, inn.CustomTextSource4, inn.MaxY, inn.MaxX, drawingGroup, dc,
                    inn.FontSize, inn.FontFamilyName);
                var dispatcherOperation = roslynCodeControl.SecondaryDispatcher.InvokeAsync(
                    new Func<LineInfo>(() => Callback3(inn2)),
                    DispatcherPriority.Send, CancellationToken.None);
                var lineInfo2 = await dispatcherOperation.Task.ConfigureAwait(true);
                roslynCodeControl.InsertionLine = lineInfo2;
                roslynCodeControl.UpdateCaretPosition();
            }
            else
            {
                roslynCodeControl.InsertionLine = lineInfo;
            }

            roslynCodeControl.ChangingText = true;
            Debug.WriteLine("About to update source text");
            roslynCodeControl.SourceText = code;
            Debug.WriteLine("Done updating source text");
            roslynCodeControl.ChangingText = false;
        }


        private static LineInfo Callback3(InClassName inn)
        {
            try
            {
                inn.CurrentRendering = FontRendering.CreateInstance(inn.FontSize, TextAlignment.Left,
                    new TextDecorationCollection(), Brushes.Black,
                    new Typeface(new FontFamily(inn.FontFamilyName), FontStyles.Normal, FontWeights.Normal,
                        FontStretches.Normal));
                var lineInfo = RedrawLine((InClassName) inn, out var lineCtx);
                return lineInfo;
            }
            catch (Exception ex)

            {
                Debug.WriteLine(ex.ToString());
            }

            return null;
        }

        private LineInfo Callback(InClassName inn, int insertionPoint, InputRequest inputRequest)
        {
            var text = inputRequest.Text;
            try
            {
                inn.CurrentRendering = FontRendering.CreateInstance(inn.FontSize, TextAlignment.Left,
                    new TextDecorationCollection(), Brushes.Black,
                    new Typeface(new FontFamily(inn.FontFamilyName), FontStyles.Normal, FontWeights.Normal,
                        FontStretches.Normal));
                CustomTextSource.TextInput(insertionPoint, inputRequest);

                var lineInfo = RedrawLine((InClassName) inn, out var lineCtx);
                if (inputRequest.Kind == InputRequestKind.NewLine)
                {
                }

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
                var textDest = inClassName.RoslynCodeControl._textDest;
                var i = inClassName.LineNo / 100;
                var j = inClassName.LineNo % 100;
                if (textDest.Children.Count <= i)
                {
                    var drawingGroup = new DrawingGroup();
                    for (var k = 0; k < j; k++) drawingGroup.Children.Add(new DrawingGroup());
                    drawingGroup.Children.Add(inClassName.D);
                    textDest.Children.Add(drawingGroup);
                }
                else
                {
                    var drawingGroup = (DrawingGroup) textDest.Children[i];
                    for (var k = 0; k < j; k++) drawingGroup.Children.Add(new DrawingGroup());

                    if (j >= drawingGroup.Children.Count)
                        drawingGroup.Children.Add(inClassName.D);
                    else
                        drawingGroup.Children[j] = inClassName.D;
                }


                inClassName.RoslynCodeControl.MaxX = Math.Max(inClassName.RoslynCodeControl.MaxX, lineCtxMaxX);

                inClassName.RoslynCodeControl.MaxY = Math.Max(inClassName.RoslynCodeControl.MaxY, lineCtxMaxY);
                inClassName.RoslynCodeControl.Rectangle.Width = lineCtxMaxX;
                inClassName.RoslynCodeControl.Rectangle.Height = lineCtxMaxY;
                inClassName.RoslynCodeControl._rect2.Width = lineCtxMaxX;
                inClassName.RoslynCodeControl._rect2.Height = lineCtxMaxY;
            });

            return outLineInfo;
        }

        /// <summary>
        /// 
        /// </summary>
        private static Typeface CreateTypeface(FontFamily fontFamily, FontStyle fontStyle, FontStretch fontStretch,
            FontWeight fontWeight)
        {
            return new Typeface(fontFamily, fontStyle, fontWeight, fontStretch);
        }

        protected override void OnTemplateChanged(ControlTemplate oldTemplate, ControlTemplate newTemplate)
        {
            base.OnTemplateChanged(oldTemplate, newTemplate);
            Debug.WriteLine($"{newTemplate}");
        }

        /// <summary>
        /// 
        /// </summary>
        // ReSharper disable once MemberCanBePrivate.Global
        protected static TextFormatter Formatter { get; } = TextFormatter.Create();

        /// <inheritdoc />
        protected override Size ArrangeOverride(Size arrangeBounds)
        {
            _nliens = (int) (arrangeBounds.Height / (FontFamily.LineSpacing * FontSize));

            var arrangeOverride = base.ArrangeOverride(arrangeBounds);
            var scrollBar = (ScrollBar) _scrollViewer.Template.FindName("PART_VerticalScrollBar", _scrollViewer);

            OutputWidth = _scrollViewer.ActualWidth - scrollBar.ActualWidth - Rectangle.StrokeThickness * 2;
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

            Debug.WriteLine("Enteirng updateformattedtext " + PerformingUpdate);
            if (PerformingUpdate)
            {
                Debug.WriteLine("Already performing update");
                return;
            }

            PerformingUpdate = true;
            RaiseEvent(new RoutedEventArgs(RenderStartEvent, this));

            var textStorePosition = 0;
            var linePosition = new Point(_xOffset, 0);

            _textDest.Children.Clear();

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
            Rectangle.Width = OutputWidth + Rectangle.StrokeThickness * 2;
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
                            var xx1 = task.Exception?.Flatten().ToString() ?? "";
                            Debug.WriteLine(xx1);
                            Debug.WriteLine(task.Exception.ToString());
                        }

                        return task.Result;
                    }).ConfigureAwait(true);
            InnerUpdateDispatcherOperation = null;
            _scrollViewer.VerticalScrollBarVisibility = ScrollBarVisibility.Visible;
            CustomTextSource = source;
            Debug.WriteLine("Return from await inner update");

            Debug.WriteLine("Setting reactangle width to " + MaxX);

            PerformingUpdate = false;
            InitialUpdate = false;
            RaiseEvent(new RoutedEventArgs(RenderCompleteEvent, this));
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
                using (var sr = File.OpenText(newValue))
                {
                    var code = await sr.ReadToEndAsync();
                    SourceText = code;
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
            var tf = CreateTypeface(new FontFamily(faceName), FontStyles.Normal,
                FontStretches.Normal,
                FontWeights.Normal);

            var currentRendering = FontRendering.CreateInstance(emSize0,
                TextAlignment.Left,
                null,
                Brushes.Black,
                tf);
            var customTextSource4 =
                roslynCodeControl.CreateAndInitTextSource(pixelsPerDip, tf, tree, node0, compilation, s1, emSize0);

            var startTime = DateTime.Now;
            var myGroup = new DrawingGroup();
            var myDc = myGroup.Open();
            var lineContext = new LineContext();
            var genericTextParagraphProperties =
                new GenericTextParagraphProperties(currentRendering, pixelsPerDip);
            var runsInfos = new List<Tuple<TextRun, Rect>>();
            var allCharInfos = new List<CharInfo>(80 * 100);
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
                    var linec = 0;
                    var xOrigin = linePosition.X;
                    var charInfos = new List<CharInfo>(myTextLine.Length);
                    foreach (var glyphRunC in indexedGlyphRuns)
                    {
                        var gl = glyphRunC.GlyphRun;
                        var advanceSum = gl.AdvanceWidths.Sum();

                        for (var i = 0; i < gl.Characters.Count; i++)
                        {
                            var glAdvanceWidth = gl.AdvanceWidths[i];
                            var glCharacter = gl.Characters[i];
                            var glCaretStop = gl.CaretStops?[i];
                            var ci = new CharInfo(textStorePosition + linec, linec, i, glCharacter, glAdvanceWidth,
                                glCaretStop, xOrigin, linePosition.Y);
                            linec++;
                            xOrigin += glAdvanceWidth;
                            charInfos.Add(ci);
                            allCharInfos.Add(ci);
                        }

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
                    line++;

                    prev = null;

                    // Update the index position in the text store.
                    textStorePosition += myTextLine.Length;
                }

                if (line > 0 && line % 100 == 0)
                {
                    myDc.Close();
                    myGroup.Freeze();
                    var curUi = new UpdateInfo() {DrawingGroup = myGroup, CharInfos = allCharInfos.ToList()};
                    channelWriter.WriteAsync(curUi);
                    myGroup = new DrawingGroup();
                    myDc = myGroup.Open();
                }
            }


            if (line % 100 != 0)
            {
                myDc.Close();
                myGroup.Freeze();
                var curUi = new UpdateInfo() {DrawingGroup = myGroup, CharInfos = allCharInfos.ToList()};
                channelWriter.WriteAsync(curUi);
            }
            else
            {
                myDc.Close();
            }

            customTextSource4.RunInfos = runsInfos;
            return customTextSource4;
        }

        private void UpdateCaretPosition(int? oldValue = null, int? newValue = null)
        {
            Debug.WriteLine($"{nameof(UpdateCaretPosition)} ( {oldValue} , {newValue} )");
            var insertionPoint = newValue ?? InsertionPoint;
            var forward = true;
            if (oldValue.HasValue && newValue.HasValue) forward = newValue.Value > oldValue.Value;

            Debug.WriteLine($"forward = {forward}");
            int ciIndex;
            if (forward)
                ciIndex = CharInfos.FindIndex(ci0 =>
                    ci0.Index >= insertionPoint);
            else
                ciIndex = CharInfos.FindLastIndex(ci0 =>
                    ci0.Index <= insertionPoint);

            Debug.WriteLine($"Found index {ciIndex}");
            CharInfo ci = null;
            if (forward && ciIndex >= 1)
                ciIndex--;
            else if (forward && ciIndex == -1)
                ciIndex = CharInfos.Count - 1;
            else if (forward) ciIndex++;

            ci = CharInfos[ciIndex];
            Debug.WriteLine($"Character is {ci.Character}");
            if (forward)
            {
                if (ci.Index != insertionPoint - 1)
                {
                    ci = CharInfos[ciIndex + 1];
                    _textCaret.SetValue(Canvas.TopProperty, ci.YOrigin);
                    _textCaret.SetValue(Canvas.LeftProperty, ci.XOrigin);
                    _updatingCaret = true;
                    InsertionPoint = ci.Index;
                    _updatingCaret = false;
                    InsertionCharInfo = ci;
                }
                else
                {
                    
                    _textCaret.SetValue(Canvas.TopProperty, ci.YOrigin);
                    _textCaret.SetValue(Canvas.LeftProperty, ci.XOrigin + ci.AdvanceWidth);
                    if (CharInfos.Count > ciIndex + 1 && CharInfos[ciIndex + 1].Index == ci.Index + 1)
                    {
                        InsertionCharInfo = CharInfos[ciIndex + 1];
                    }
                    else
                    {
                        InsertionCharInfo = null;
                    }
                }
            }
            else
            {
                if (ci.Index != insertionPoint)
                {
                    _textCaret.SetValue(Canvas.TopProperty, ci.YOrigin);
                    _textCaret.SetValue(Canvas.LeftProperty, ci.XOrigin + ci.AdvanceWidth);
                    _updatingCaret = true;
                    InsertionPoint = ci.Index + 1;
                    _updatingCaret = false;
                    InsertionCharInfo = null;
                }
                else
                {
                    var ciYOrigin = ci.YOrigin;

                    _textCaret.SetValue(Canvas.TopProperty, ciYOrigin);
                    var leftValue = ci.XOrigin;
                    Debug.WriteLine($"New caret position is ( {leftValue} , {ciYOrigin} )");
                    _textCaret.SetValue(Canvas.LeftProperty, leftValue);
                    InsertionCharInfo = ci;
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
        private ScrollViewer _scrollViewer;

        /// <summary>
        /// 
        /// </summary>
        public Typeface Typeface { get; protected set; }

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
        private CustomTextSource4 _store;

        private int _selectionEnd;
        private SyntaxNode _startNode;
        private SyntaxNode _endNode;
        private SourceText _text;
        private int _nliens;
        private ObjectAnimationUsingKeyFrames _x1;
        private CustomTextSource4 _customTextSource;
        private double _xOffset = 0.0;
        private ISymbol _enclosingSymbol;
        private DispatcherOperation<Task> _updateOperation;
        private bool _performingUpdate;
        private DispatcherOperation<CustomTextSource4> _innerUpdateDispatcherOperation;
        private Task _updateFormattedTestTask;
        private ChannelReader<UpdateInfo> _reader;
        private Channel<UpdateInfo> _channel;
        private bool _handlingInput;
        private bool _updatingCaret;

        /// <inheritdoc />
        protected override void OnMouseMove(MouseEventArgs e)
        {
            DrawingContext dc = null;
            try
            {
                var point = e.GetPosition(Rectangle);
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
                            Rectangle.CaptureMouse();
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

                Rectangle.ReleaseMouseCapture();
            }
        }

        /// <inheritdoc />
        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            InsertionPoint = HoverOffset;
        }

        private static void OnNodeUpdated(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var ss = (RoslynCodeControl) d;
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
                _scrollViewer?.ScrollToTop();
                if (SecondaryDispatcher != null)
                    await UpdateTextSource();

                //UpdateFormattedText();
            }
        }

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

        public Rectangle Rectangle { get; set; }

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

    public class CharInfo
    {
        public int LineIndex { get; }
        public int RunIndex { get; }
        public char Character { get; }
        public double AdvanceWidth { get; }
        public bool? CaretStop { get; }
        public double XOrigin { get; }
        public double YOrigin { get; }
        public int Index { get; set; }

        public CharInfo(in int index, in int lineIndex, in int runIndex, char character, double advanceWidth,
            bool? caretStop,
            double xOrigin, double yOrigin)
        {
            Index = index;
            LineIndex = lineIndex;
            RunIndex = runIndex;
            Character = character;
            AdvanceWidth = advanceWidth;
            CaretStop = caretStop;
            XOrigin = xOrigin;
            YOrigin = yOrigin;
        }
    }

    internal class In2
    {
        public RoslynCodeControl RoslynCodeControl { get; }
        public int InsertionPoint { get; }
        public string Code { get; }
        public InputRequest InputRequest { get; }
        public string Text { get; }
        public InClassName In1 { get; }
        public LineInfo LineInfo { get; }

        public In2(RoslynCodeControl roslynCodeControl, in int insertionPoint, string code, InputRequest inputRequest,
            string text, InClassName in1, LineInfo lineInfo)
        {
            RoslynCodeControl = roslynCodeControl;
            InsertionPoint = insertionPoint;
            Code = code;
            InputRequest = inputRequest;
            Text = text;
            In1 = in1;
            LineInfo = lineInfo;
        }
    }

    public enum InputRequestKind
    {
        TextInput,
        NewLine,
        Backspace
    }

    public class UpdateInfo
    {
        public BitmapSource ImageSource { get; set; }
        public Rect Rect { get; set; }
        public DrawingGroup DrawingGroup { get; set; }
        public List<CharInfo> CharInfos { get; set; }
    }

    public class InputRequest
    {
        private readonly string _text;
        public InputRequestKind Kind { get; }

        public string Text
        {
            get
            {
                return Kind == InputRequestKind.TextInput ? _text : Kind == InputRequestKind.NewLine ? "\r\n" : null;
            }
        }

        public InputRequest(InputRequestKind kind, string text)
        {
            Kind = kind;
            _text = text;
        }

        public InputRequest(InputRequestKind kind)
        {
            Kind = kind;
        }
    }
}