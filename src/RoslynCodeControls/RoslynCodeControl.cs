#define MOUSE
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.TextFormatting;
using System.Windows.Shapes;
using System.Windows.Threading;
using JetBrains.Annotations;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Threading;
using CSharpExtensions = Microsoft.CodeAnalysis.CSharp.CSharpExtensions;
using TextChange = Microsoft.CodeAnalysis.Text.TextChange;
// ReSharper disable UnusedParameter.Local

// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
#pragma warning disable 162

// ReSharper disable ConvertToUsingDeclaration


namespace RoslynCodeControls
{
    /// <summary>
    /// 
    /// </summary>
    public sealed class RoslynCodeControl : RoslynCodeBase, ILineDrawer, INotifyPropertyChanged, IFace1, ICodeView
    {
        /// <inheritinpc />
        public RoslynCodeControl() : this(null)
        {
        }

        public RoslynCodeControl(Action<string> debugOut = null) : base(debugOut)
        {

            _typefaceName = FontFamily.FamilyNames[XmlLanguage.GetLanguage("en-US")];
            _textDestination = new DrawingGroup();
            _myDrawingBrush = new DrawingBrush()
            {
                AlignmentX = AlignmentX.Left,
                AlignmentY = AlignmentY.Top,
                TileMode = TileMode.None,
                ViewboxUnits = BrushMappingMode.Absolute,
                Stretch = Stretch.None,
                Drawing = _textDestination
            };

            if (_doBinding)
                BindingOperations.SetBinding(_myDrawingBrush, TileBrush.ViewboxProperty,
                    new Binding("DrawingBrushViewbox") {Source = this});

            // _documentPaginator = new RoslynPaginator(this);
            UpdateCompleteChannel = Channel.CreateUnbounded<UpdateComplete>(new UnboundedChannelOptions()
                {SingleReader = true, SingleWriter = true});
            Rectangle = new Rectangle();
            if (_doBinding)
            {
                Rectangle.SetBinding(WidthProperty,
                    new Binding("DrawingBrushViewbox.Width") {Source = this});
                Rectangle.SetBinding(HeightProperty,
                    new Binding("DrawingBrushViewbox.Height") {Source = this});
            }

            RenderChannel = Channel.CreateUnbounded<RenderRequest>(new UnboundedChannelOptions()
                {SingleReader = true, SingleWriter = true});
            PostUpdateChannel = Channel.CreateUnbounded<PostUpdateRequest>(new UnboundedChannelOptions()
                {SingleReader = true, SingleWriter = true});

            UpdateChannel = Channel.CreateUnbounded<UpdateInfo>(new UnboundedChannelOptions()
                {SingleReader = true, SingleWriter = true});

            _joinableTaskContext =
                new JoinableTaskContext(Dispatcher.Thread, new DispatcherSynchronizationContext(Dispatcher));
            _taskCollection = _joinableTaskContext.CreateCollection();
            _myJoinableTaskFactory = _joinableTaskContext.CreateFactory(_taskCollection);


            _postUpdateReaderJoinableTask = _myJoinableTaskFactory.RunAsync(StartPostUpdateReaderAsync);

            _postUpdateReaderFaultContinuation = _postUpdateReaderJoinableTask.Task.ContinueWith(FaultHandler, null,
                CancellationToken.None, TaskContinuationOptions.OnlyOnFaulted, TaskScheduler.FromCurrentSynchronizationContext());

            _x1 = new ObjectAnimationUsingKeyFrames
            {
                RepeatBehavior = RepeatBehavior.Forever,
                Duration = new Duration(TimeSpan.FromSeconds(1))
            };

            var c = new ObjectKeyFrameCollection
            {
                new DiscreteObjectKeyFrame(Visibility.Visible),
                new DiscreteObjectKeyFrame(Visibility.Hidden, KeyTime.FromPercent(.6)),
                new DiscreteObjectKeyFrame(Visibility.Visible, KeyTime.FromPercent(.4))
            };
            _x1.KeyFrames = c;


            PixelsPerDip = VisualTreeHelper.GetDpi(this).PixelsPerDip;

            SetupCommands(this, this);
        }

        private void FaultHandler(Task arg1, object arg2)
        {
            DebugFn("Faulted !!!");
            if (!arg1.IsFaulted) return;

            if (!Dispatcher.CheckAccess()) DebugFn("Wrong thread");

            IsFaulted = true;
            Exception1 = arg1.Exception;
        }

        private async Task StartPostUpdateReaderAsync()
        {
            while (!PostUpdateChannel.Reader.Completion.IsCompleted)
            {
                DebugFn("awaiting post update");
                PostUpdateRequest r;
                try
                {
                    r = await PostUpdateChannel.Reader.ReadAsync();
                }
                catch (ChannelClosedException)
                {
                    break;
                }

                DebugFn("Got post update");
                var @in = r.Input;

                var inputRequest = @in.InputRequest;
                var insertionPoint = @in.InsertionPoint;
                var text = inputRequest.Text;
                var ip = inputRequest.Kind == InputRequestKind.Backspace
                    ? insertionPoint - 1
                    : insertionPoint + (text?.Length ?? 0);

                PostUpdate(@in, ip);
#if DEBUG
                DebugFn("Writing update complete " + @in);
#endif
                var updateComplete = new UpdateComplete(@in.InputRequest, ip, @in.Timestamp, r.RenderRequestTimestamp,
                    @in.RedrawLineResult.BeganTimestamp, @in.RedrawLineResult.Timestamp);
                await UpdateCompleteChannel.Writer.WriteAsync(updateComplete);
            }

            DebugFn("exiting post update");
        }

        public Channel<PostUpdateRequest> PostUpdateChannel { get; }
        public Channel<RenderRequest> RenderChannel { get; }

        public static readonly DependencyProperty FilenameProperty = DependencyProperty.Register(
            "Filename", typeof(string), typeof(RoslynCodeBase),
            new PropertyMetadata(default(string), OnFilenameChanged));

        public static readonly DependencyProperty InsertionPointProperty = DependencyProperty.Register(
            "InsertionPoint", typeof(int), typeof(RoslynCodeControl),
            new PropertyMetadata(default(int), OnInsertionPointChanged)); //CoerceInsertionPoint));

        public static readonly DependencyProperty InsertionCharInfoProperty = DependencyProperty.Register(
            "InsertionCharInfo", typeof(CharInfo), typeof(RoslynCodeControl), new PropertyMetadata(default(CharInfo)));

        public static readonly DependencyProperty HoverOffsetProperty = DependencyProperty.Register(
            "HoverOffset", typeof(int), typeof(RoslynCodeControl), new PropertyMetadata(default(int)));


        public static readonly DependencyProperty HoverColumnProperty = DependencyProperty.Register(
            "HoverColumn", typeof(int), typeof(RoslynCodeControl), new PropertyMetadata(default(int)));

        public static readonly DependencyProperty HoverTokenProperty = DependencyProperty.Register(
            "HoverToken", typeof(SyntaxToken?), typeof(RoslynCodeControl),
            new PropertyMetadata(default(SyntaxToken?)));

        /// <summary>
        /// 
        /// </summary>
        public static readonly DependencyProperty HoverSymbolProperty = DependencyProperty.Register(
            "HoverSymbol", typeof(ISymbol), typeof(RoslynCodeControl), new PropertyMetadata(default(ISymbol)));

        public static readonly DependencyProperty HoverSyntaxNodeProperty = DependencyProperty.Register(
            "HoverSyntaxNode", typeof(SyntaxNode), typeof(RoslynCodeControl),
            new PropertyMetadata(default(SyntaxNode), new PropertyChangedCallback(OnHoverSyntaxNodeUpdated)));

        public static readonly DependencyProperty HoverRowProperty = DependencyProperty.Register(
            "HoverRow", typeof(int), typeof(RoslynCodeControl), new PropertyMetadata(default(int)));

        public static readonly DependencyProperty TextSourceTextProperty = DependencyProperty.Register(
            "TextSourceText", typeof(string), typeof(RoslynCodeControl),
            new PropertyMetadata(default(string), OnTextSourceTextChanged));

        public static readonly DependencyProperty DrawingBrushViewboxProperty = DependencyProperty.Register(
            "DrawingBrushViewbox", typeof(Rect), typeof(RoslynCodeControl),
            new PropertyMetadata(default(Rect), OnDrawingBrushViewboxChanged));

        /// <summary>
        /// 
        /// </summary>
        public static readonly DependencyProperty HoverRegionInfoProperty = DependencyProperty.Register(
            "HoverRegionInfo", typeof(RegionInfo), typeof(RoslynCodeControl),
            new PropertyMetadata(default(RegionInfo)));

        public string Filename
        {
            get { return (string) GetValue(FilenameProperty); }
            set { SetValue(FilenameProperty, value); }
        }

        private static void OnFilenameChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((RoslynCodeControl) d).OnFilenameChanged((string) e.OldValue, (string) e.NewValue);
        }


        public LinkedListNode<CharInfo> InsertionCharInfoNode { get; set; }
        // {
        // get { return _insertionCharInfoNode; }
        // set
        // {
        // if (Equals(value, _insertionCharInfoNode)) return;
        // _insertionCharInfoNode = value;
        // OnPropertyChanged();
        // }
        // }


        // ReSharper disable once UnusedMember.Local
        private static object CoerceInsertionPoint(DependencyObject d, object basevalue)
        {
            var p = (int) basevalue;
            var r = (RoslynCodeControl) d;
            if (p < 0) return 0;

            var len = r.CustomTextSource.Length;
            if (len < p)
            {
                if (len < 0) return 0;
                return len;
            }

            return p;
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
                if (SymbolEqualityComparer.Default.Equals(value, _enclosingSymbol)) return;
                _enclosingSymbol = value;
                OnPropertyChanged();
            }
        }

        private void OnInsertionPointChanged(int oldValue, int newValue)
        {
            if (newValue == -1)
            {
            }

            if (!_updatingCaret)
                UpdateCaretPosition(oldValue, newValue);

#if true
            try
            {
                if (SyntaxNode != null && !SyntaxNode.FullSpan.Contains(newValue)) return;


                var enclosingSymbol = SemanticModel?.GetEnclosingSymbol(newValue);
                EnclosingSymbol = enclosingSymbol;

                if (EnclosingSymbol != null)
#if DEBUG
                    _debugFn?.Invoke("Enclosing symbol " +
                                     EnclosingSymbol.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat) +
                                     " " + EnclosingSymbol.Kind);
#endif
                if (SemanticModel != null) // && InsertionRegion.SyntaxNode != null)
                {
                    var nodeOrToken = SyntaxNode.ChildThatContainsPosition(newValue);
                    if (nodeOrToken.IsNode)
                    {
                        var syntaxNode = nodeOrToken.AsNode();
                        SyntaxNodeOrToken x = null;
                        while (syntaxNode != null)
                        {
                            x = syntaxNode.ChildThatContainsPosition(newValue);
                            syntaxNode = x.AsNode();
                        }

                        syntaxNode = x.Parent;
                        if (syntaxNode.Span.Contains(newValue))
                        {
                            DebugFn(CSharpExtensions.Kind(syntaxNode).ToString());
                            DebugFn(syntaxNode.ToString());
                            var ti = SemanticModel.GetTypeInfo(syntaxNode);
                            TypeInfo = ti;
                            if (ti.Type != null)
                            {
                                TypeInfo = ti;
#if DEBUG
                                _debugFn?.Invoke("type info: " +
                                                 ti.Type.ToDisplayString(SymbolDisplayFormat
                                                     .MinimallyQualifiedFormat));

#endif
                            }
                        }
                        else
                        {
                            TypeInfo = default(TypeInfo);
                        }
                    }
                }
            }
            catch (Exception)
            {
                // ignored
            }
#endif
        }

        public TypeInfo TypeInfo
        {
            get { return _typeInfo; }
            set
            {
                if (value.Equals(_typeInfo)) return;
                _typeInfo = value;
                OnPropertyChanged();
            }
        }


        /// <summary>
        /// 
        /// </summary>
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
        private static void OnHoverSyntaxNodeUpdated(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
        }

        /// <summary>
        /// 
        /// </summary>
        public SyntaxNode HoverSyntaxNode
        {
            get { return (SyntaxNode) GetValue(HoverSyntaxNodeProperty); }
            set { SetValue(HoverSyntaxNodeProperty, value); }
        }


        public int HoverColumn
        {
            get { return (int) GetValue(HoverColumnProperty); }
            set { SetValue(HoverColumnProperty, value); }
        }


        /// <summary>
        /// 
        /// </summary>
        public int HoverRow
        {
            get { return (int) GetValue(HoverRowProperty); }
            set { SetValue(HoverRowProperty, value); }
        }

        private async void SourceOnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "Text")
            {
                var textSourceText = CustomTextSource.Text.ToString();
                await Dispatcher.InvokeAsync(() => { TextSourceText = textSourceText; });
            }
        }

        public string TextSourceText
        {
            get { return (string) GetValue(TextSourceTextProperty); }
            set { SetValue(TextSourceTextProperty, value); }
        }

        private static void OnTextSourceTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((RoslynCodeControl) d).OnTextSourceTextChanged((string) e.OldValue, (string) e.NewValue);
        }


        private void OnTextSourceTextChanged(string oldValue, string newValue)
        {
        }

        #region Private members

        private readonly DrawingBrush _myDrawingBrush;

        // ReSharper disable once NotAccessedField.Local
        private int _startColumn;

        // ReSharper disable once NotAccessedField.Local
        private int _startRow;
        private int _startOffset;

        private Rectangle _rect2;
        private DrawingGroup _dg2;
        private Grid _innerGrid;
        private TextCaret _textCaret;
        private Canvas _canvas;

        private int _selectionEnd;
        private SyntaxNode _startNode;
        private SyntaxNode _endNode;
        private SourceText _text;
        private ObjectAnimationUsingKeyFrames _x1;
        private ISymbol _enclosingSymbol;
        private DispatcherOperation<Task> _updateOperation;
        private bool _handlingInput;
        private bool _updatingCaret;
        private Rectangle _rectangle;
        private LinkedListNode<CharInfo> _insertionCharInfoNode;
        private LinkedListNode<LineInfo2> _insertionLineNode;
        private Rect _drawingBrushViewbox;
        private readonly DrawingGroup _textDestination;

        #endregion

        #region Public properties

        public Channel<UpdateComplete> UpdateCompleteChannel { get; }

        public LineInfo2 FirstLine
        {
            get { return LineInfos2?.First?.Value; }
        }

        #region Channel tasks

        private async Task RenderChannelReaderAsync()
        {
            while (!RenderChannel.Reader.Completion.IsCompleted)
            {
                RenderRequest inp;
                try
                {
                    inp = await RenderChannel.Reader.ReadAsync();
                }
                catch (ChannelClosedException)
                {
                    break;
                }

                await HandleRenderRequestAsync(inp);
            }

            DebugFn("exiting render channel");
            // ReSharper disable once FunctionNeverReturns
        }

        #endregion

        private async Task HandleRenderRequestAsync(RenderRequest renderRequest)
        {
#if DEBUG
            DebugFn($"{nameof(HandleRenderRequestAsync)}: {renderRequest}");
#endif
            var inn = renderRequest.Input;
            if (renderRequest.Input.CustomTextSource4.CurrentRendering == null)
                renderRequest.Input.CustomTextSource4.CurrentRendering = FontRendering.CreateInstance(inn.FontSize,
                    TextAlignment.Left,
                    new TextDecorationCollection(), Brushes.Black,
                    new Typeface(new FontFamily(inn.FontFamilyName), FontStyles.Normal, inn.FontWeight,
                        FontStretches.Normal));

            TextChange? change = null;
            if (renderRequest.InputRequest != null)
                try
                {
                    DebugFn("Calling TextInputAsync");
                    change = await inn.CustomTextSource4.TextInputAsync(renderRequest.InsertionPoint,
                        renderRequest.InputRequest, renderRequest.LineInfo?.Offset ?? 0);
                    DebugFn("returned from TextInputAsync");
                }
                catch (Exception ex)
                {
                    DebugFn("exception");
                    throw new CodeControlFaultException("Text source input method failed", ex);
                }

            var redrawLine = RedrawLine(inn, renderRequest.Input.CustomTextSource4.CurrentRendering, change,
                renderRequest.LineInfo);

            redrawLine.DrawingGroup.Freeze();
            var postUpdateInput = new PostUpdateInput(this,
                renderRequest.InsertionPoint, renderRequest.InputRequest,
                redrawLine);
            var postUpdateRequest = new PostUpdateRequest(postUpdateInput, renderRequest.Timestamp);
            DebugFn("writing post update request");
            await PostUpdateChannel.Writer.WriteAsync(postUpdateRequest);
            DebugFn("Here");
        }

        #endregion

        #region Overrides

        public override DrawingBrush DrawingBrush
        {
            get { return _myDrawingBrush; }
        }

        /// <inheritdoc />
        protected override void SecondaryThreadTasks()
        {
            _renderChannelReaderTask = RenderChannelReaderAsync().ContinueWith(t =>
            {
                if (!t.IsFaulted) return;
                _debugFn?.Invoke(t.Exception.Flatten().ToString());
                JTF.Run(async () =>
                {
                    await JTF.SwitchToMainThreadAsync();
                    FaultedTask = t;
                    IsFaulted = true;   
                    Exception1 = t.Exception;
                });
            });
        }

        public Task FaultedTask { get; set; }

        public AggregateException Exception1 { get; set; }

        public bool IsFaulted { get; set; }

        protected override void OnPreviewKeyDown(KeyEventArgs e)
        {
            base.OnPreviewKeyDown(e);
            if (e.Handled) return;
            switch (e.Key)
            {
                case Key.Escape:
                    e.Handled = true;
                    HandleFault(Task.FromException(new CodeControlFaultException("test fault")));
                    break;
                case Key.Space:
                    if (e.KeyboardDevice.Modifiers == ModifierKeys.Control)
                    {
                        JTF.RunAsync(DoCompletionAsync).Task.ContinueWith(HandleFault, CancellationToken.None,
                            TaskContinuationOptions.OnlyOnFaulted, TaskScheduler.FromCurrentSynchronizationContext());
                    }

                    break;

#if DEBUG
                case Key.F1:
                case Key.D1:
                    if (e.KeyboardDevice.Modifiers == ModifierKeys.Control)
                    {
                        e.Handled = true;
                        if (Debug1Container != null)
                        {
                            if (Debug1Container.Visibility != Visibility.Visible)
                            {
                                Debug1Container.Visibility = Visibility.Visible;
                                Debug2Container.Visibility = Visibility.Hidden;
                                Debug3Container.Visibility = Visibility.Hidden;
                            }
                            else
                            {
                                Debug1Container.Visibility = Visibility.Hidden;
                            }
                        }
                    }

                    break;
                case Key.F2:
                case Key.D2:
                    if (e.KeyboardDevice.Modifiers == ModifierKeys.Control)
                    {
                        e.Handled = true;
                        if (Debug2Container != null)
                        {
                            if (Debug2Container.Visibility != Visibility.Visible)
                            {
                                Debug2Container.Visibility = Visibility.Visible;
                                Debug1Container.Visibility = Visibility.Hidden;
                                Debug3Container.Visibility = Visibility.Hidden;
                            }
                            else
                            {
                                Debug2Container.Visibility = Visibility.Hidden;
                            }
                        }
                    }

                    break;
                case Key.F3:
                case Key.D3:
                    if (e.KeyboardDevice.Modifiers == ModifierKeys.Control)
                    {
                        e.Handled = true;
                        if (Debug3Container != null)
                        {
                            if (Debug3Container.Visibility != Visibility.Visible)
                            {
                                Debug3Container.Visibility = Visibility.Visible;
                                Debug1Container.Visibility = Visibility.Hidden;
                                Debug2Container.Visibility = Visibility.Hidden;
                            }
                            else
                            {
                                Debug3Container.Visibility = Visibility.Hidden;
                            }
                        }
                    }

                    break;
#endif
                case Key.Left:
                    e.Handled = true;
                    if (CanMoveLeftByCharacter()) MoveLeftByCharacter();

                    break;

                case Key.Up:
                    e.Handled = true;
                    if (CanMoveUpByLine()) MoveUpByLine();

                    break;
                case Key.Right:
                    e.Handled = true;
                    if (CanMoveRightByCharacter()) MoveRightByCharacter();

                    break;
                case Key.Down:
                    e.Handled = true;
                    if (CanMoveDownByLine()) MoveDownByLine();

                    break;
            }
        }

        private void HandleFault(Task obj)
        {
            if (!obj.IsFaulted)
                return;
            var ex = obj.Exception;
            Popup errPopup = new Popup();
            var errPopupChild = new Border
            {
                Padding = new Thickness(25),
                Background = Brushes.Red,
                BorderBrush = Brushes.Black,
                BorderThickness = new Thickness(5),
                CornerRadius = new CornerRadius(3),
                Child = new TextBlock
                {
                    FontSize = 28.0,
                    Text = ex.Flatten().Message,
                    TextWrapping = TextWrapping.Wrap,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                }
            };
            errPopup.Child = errPopupChild;
            
            errPopup.PlacementTarget = this;
            errPopup.Placement = PlacementMode.Center;
            errPopup.StaysOpen = true;
            errPopup.IsOpen = true;
            BooleanKeyFrameCollection kf = new BooleanKeyFrameCollection {new DiscreteBooleanKeyFrame(true, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(3))), new DiscreteBooleanKeyFrame(false)};
            BooleanAnimationUsingKeyFrames tl = new BooleanAnimationUsingKeyFrames(){KeyFrames = kf,FillBehavior = FillBehavior.HoldEnd};
            errPopup.BeginAnimation(Popup.IsOpenProperty, tl);

        }

        private async Task DoCompletionAsync()
        {
            var completionService = CompletionService.GetService(Document);
            var results = await completionService.GetCompletionsAsync(Document, InsertionPoint);
            var listBx = false;
            ItemsControl child;
            var rPopup = new Popup
            {
                PlacementTarget = _textCaret,
                Placement = PlacementMode.Top
                // PlacementRectangle = new Rect(Canvas.GetLeft(_textCaret), Canvas.GetTop(_textCaret), 10, 10)
            };

            // ReSharper disable once ConditionIsAlwaysTrueOrFalse
            if (listBx)
            {
                var rListbox = new ListBox();
                rListbox.ItemsSource = results.Items;
                child = rListbox;
            }
            else
            {
                var combo = new ComboBox {IsDropDownOpen = true, ItemsSource = results.Items};
                combo.MaxDropDownHeight = 300;
                combo.StaysOpenOnEdit = true;
                combo.IsEditable = true;
                combo.MinWidth = 120;
                combo.SelectionChanged += (sender, args) =>
                {
                    DebugFn("selected item is " + combo.SelectedItem ?? "null");
                };
                combo.AddHandler(PreviewKeyDownEvent, new KeyEventHandler((sender, args) =>
                {
                    if (args.Key != Key.Tab || combo.SelectedItem == null) return;
                    args.Handled = true;
                    var comboSelectedItem = (CompletionItem) combo.SelectedItem;
                    var text = comboSelectedItem
                        .ToString(); //comboSelectedItem.Properties["InsertionText"];
                    var inputRequest = new InputRequest(InputRequestKind.TextInput,
                        text);
                    rPopup.IsOpen = false;
                    var jt = JTF.RunAsync(async () => DoInputAsync(inputRequest));
                }));
                child = combo;
            }

            rPopup.Child = child;
            rPopup.StaysOpen = true;
                rPopup.Opened += (sender, args) => Keyboard.Focus(child);

                rPopup.IsOpen = true;
            }
        

        public override DrawingGroup TextDestination
        {
            get { return _textDestination; }
        }

        // public override Rect DrawingBrushViewbox
        // {
        // get { return (Rect) GetValue(DrawingBrushViewboxProperty); }
        // set { SetValue(DrawingBrushViewboxProperty, value); }
        // }

        /// <inheritdoc />
        protected override Size MeasureOverride(Size constraint)
        {
            var measureOverride = base.MeasureOverride(constraint);
            // var w = _scrollViewer.DesiredSize.Width;
            return measureOverride;
        }

        /// <inheritdoc />
        public override void OnApplyTemplate()
        {
#if DEBUG
            Debug1Container = (UIElement) GetTemplateChild("debug1container");
            Debug2Container = (UIElement) GetTemplateChild("debug2container");
            Debug3Container = (UIElement) GetTemplateChild("debug3container");
#endif
            _scrollViewer = (ScrollViewer) GetTemplateChild("ScrollViewer");
            if (_scrollViewer != null)
                OutputWidth = _scrollViewer.ActualWidth;
            // if (IsKeyboardFocused)
            // {
            //     _focusing = true;
            //     Keyboard.Focus(_scrollViewer);
            //     _focusing = false;
            // }
            // if(OutputWidth == 0)
            // {
            // throw new InvalidOperationException();
            // }

            // ReSharper disable once SpecifyACultureInStringConversionExplicitly
#if DEBUG
            _debugFn?.Invoke(OutputWidth.ToString());
#endif

            var templateChild = (Rectangle) GetTemplateChild("Rectangle");
            if (templateChild != null)
            {
                Rectangle = templateChild;
                if (_doBinding)
                {
                    Rectangle.SetBinding(WidthProperty,
                        new Binding("DrawingBrushViewbox.Width") {Source = this});
                    Rectangle.SetBinding(HeightProperty,
                        new Binding("DrawingBrushViewbox.Height") {Source = this});
                }

                Rectangle.Fill = DrawingBrush;
            }

            Translate = (TranslateTransform) GetTemplateChild("TranslateTransform");

            _grid = (Grid) GetTemplateChild("Grid");
            _canvas = (Canvas) GetTemplateChild("Canvas");
            _innerGrid = (Grid) GetTemplateChild("InnerGrid");

            _textCaret = new TextCaret(FontSize * 1.1);

            _canvas.Children.Add(_textCaret);

            _border = (Border) GetTemplateChild("Border");

            _rect2 = (Rectangle) GetTemplateChild("Rect2");
            _dg2 = (DrawingGroup) GetTemplateChild("DG2");
        }

        protected override async void OnPreviewTextInput(TextCompositionEventArgs e)
        {
            base.OnPreviewTextInput(e);
            if (_handlingInput)
                return;
            DebugFn("*** TEXT INPUT ***");
            var eText = e.Text;
            e.Handled = true;
            try
            {
                _handlingInput = true;
                Status = CodeControlStatus.InputHandling;
                var result = await DoInputAsync(new InputRequest(InputRequestKind.TextInput, eText))
                    .ConfigureAwait(true);
                _lastInputResult = result;
            }
            finally
            {
                _handlingInput = false;
                Status = CodeControlStatus.Idle;
            }
        }

        /// <inheritdoc />
        public override JoinableTaskFactory JTF
        {
            get { return _myJoinableTaskFactory; }
        }

        public override JoinableTaskFactory JTF2
        {
            get { return _jtf2; }
            set { _jtf2 = value; }
        }

        public override bool InitialUpdate { get; set; } = true;

        public override CharInfo InsertionCharInfo
        {
            get { return (CharInfo) GetValue(InsertionCharInfoProperty); }
            set { SetValue(InsertionCharInfoProperty, value); }
        }

        public override int InsertionPoint
        {
            get { return (int) GetValue(InsertionPointProperty); }
            set { SetValue(InsertionPointProperty, value); }
        }

#if EXTRA
public override bool PerformingUpdate
        {
            get { return _performingUpdate; }
            set
            {
                if (value == _performingUpdate) return;
#if DEBUG
                _debugFn?.Invoke("Performing update set to " + value);
#endif
                _performingUpdate = value;
                OnPropertyChanged();
            }
        }
#endif

        #endregion

        static RoslynCodeControl()
        {
            FontSizeProperty.OverrideMetadata(typeof(RoslynCodeControl),
                new FrameworkPropertyMetadata(OnFontSizeChanged));
            FontFamilyProperty.OverrideMetadata(typeof(RoslynCodeControl),
                new FrameworkPropertyMetadata(OnFontFamilyChanged));
            FocusableProperty.OverrideMetadata(typeof(RoslynCodeControl), new FrameworkPropertyMetadata(true));
            DefaultStyleKeyProperty.OverrideMetadata(typeof(RoslynCodeControl),
                new FrameworkPropertyMetadata(typeof(RoslynCodeControl)));
            SyntaxTreeProperty.OverrideMetadata(typeof(RoslynCodeControl),
                new FrameworkPropertyMetadata(default(SyntaxTree), FrameworkPropertyMetadataOptions.None,
                    OnSyntaxTreeChanged_));
            SyntaxNodeProperty.OverrideMetadata(typeof(RoslynCodeControl),
                new PropertyMetadata(default(SyntaxNode), OnNodeUpdated));
        }

        private static void OnFontFamilyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var c = (RoslynCodeControl) d;
            var f = (FontFamily) e.NewValue;
            c._typefaceName = f.FamilyNames[XmlLanguage.GetLanguage("en-US")];
        }

        private static void OnFontSizeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var textCaret = ((RoslynCodeControl) d)._textCaret;
            if (textCaret == null) return;

            textCaret.lineHeight = (double) e.NewValue * 1.1;

            ((UIElement) d).InvalidateVisual();
        }

        private static void OnSyntaxTreeChanged_(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var ss = (RoslynCodeControl) d;
            ss.OnSyntaxTreeUpdated((SyntaxTree) e.NewValue);
        }

        private static void OnDrawingBrushViewboxChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((RoslynCodeControl) d).OnDrawingBrushViewboxChanged((Rect) e.OldValue, (Rect) e.NewValue);
        }

        private void OnDrawingBrushViewboxChanged(Rect oldValue, Rect newValue)
        {
        }


        #region Input handling

        public void MoveLeftByCharacter()
        {
            if (InsertionPoint > 0) InsertionPoint--;
        }

        private void CanExecuteBackspace(object sender, CanExecuteRoutedEventArgs e)
        {
            bool CanCanMoveUpByLine()
            {
                return CustomTextSource != null && !_handlingInput && InsertionPoint > 0;
            }

            e.CanExecute = CanCanMoveUpByLine();
            e.Handled = true;
        }

        private void OnMoveDownByLine(object sender, ExecutedRoutedEventArgs e)
        {
            MoveDownByLine();
        }

        public void MoveDownByLine()
        {
            var ci = InsertionCharInfo;
            if (ci != null)
            {
                var upCi = CharInfos.FirstOrDefault(ci0 =>
                    ci0.LineNumber == ci.LineNumber + 1 && ci0.XOrigin >= ci.XOrigin);
                if (upCi != null)
                {
                    InsertionPoint = upCi.Index;
                    InsertionCharInfo = upCi;
                }
                else
                {
                    upCi = CharInfos.LastOrDefault(ci0 => ci0.LineNumber == ci.LineNumber + 1);
                    if (upCi != null)
                    {
                        InsertionPoint = upCi.Index + 1;
                        InsertionCharInfo = upCi;
                    }
                    else
                    {
                    }
                }
            }
        }

        private void CanExecuteOnMoveDownByLine(object sender, CanExecuteRoutedEventArgs e)
        {
            if (e.Handled)
                return;
            e.CanExecute = CanMoveDownByLine();
            e.Handled = true;
        }

        private bool CanMoveDownByLine()
        {
            return CustomTextSource != null && !_handlingInput &&
                   InsertionCharInfo != null &&
                   CharInfos.Any(ci => ci.LineNumber >= InsertionCharInfo.LineNumber + 1);
        }

        private void CanExecuteOnMoveUpByLine(object sender, CanExecuteRoutedEventArgs e)
        {
            if (e.Handled)
                return;
            e.CanExecute = CanMoveUpByLine();
            e.Handled = true;
        }

        private bool CanMoveUpByLine()
        {
            return CustomTextSource != null && !_handlingInput && (InsertionCharInfo?.LineNumber ?? 0) > 0;
        }

        private void CanExecuteMoveRightByCharacter(object sender, CanExecuteRoutedEventArgs e)
        {
            if (e.Handled)
                return;
            e.CanExecute = CanMoveRightByCharacter();
            e.Handled = true;
        }

        private bool CanMoveRightByCharacter()
        {
            return CustomTextSource != null && !_handlingInput && InsertionPoint < CustomTextSource.Length - 2;
        }

        private void CanExecuteMoveLeftByCharacter(object sender, CanExecuteRoutedEventArgs e)
        {
            if (e.Handled)
                return;
            e.CanExecute = CanMoveLeftByCharacter();
            e.Handled = true;
        }

        private bool CanMoveLeftByCharacter()
        {
            return CustomTextSource != null && !_handlingInput && InsertionPoint > 0;
        }

        private void OnMoveUpByLine(object sender, ExecutedRoutedEventArgs e)
        {
            MoveUpByLine();
        }

        public void MoveUpByLine()
        {
            var ci = InsertionCharInfo;
            if (ci != null)
            {
                var upCi = CharInfos.FirstOrDefault(ci0 =>
                    ci0.LineNumber == ci.LineNumber - 1 && ci0.XOrigin >= ci.XOrigin);
                if (upCi != null)
                {
                    InsertionPoint = upCi.Index;
                    InsertionCharInfo = upCi;
                }
                else
                {
                    upCi = CharInfos.LastOrDefault(ci0 => ci0.LineNumber == ci.LineNumber + 1);
                    if (upCi != null)
                    {
                        InsertionPoint = upCi.Index + 1;
                        InsertionCharInfo = upCi;
                    }
                    else
                    {
                    }
                }
            }
        }

        private void OnMoveLeftByCharacter(object sender, ExecutedRoutedEventArgs e)
        {
            MoveLeftByCharacter();
        }

        private void OnMoveRightByCharacter(object sender, ExecutedRoutedEventArgs e)
        {
            MoveRightByCharacter();
        }

        public void MoveRightByCharacter()
        {
            InsertionPoint++;
        }

        private async void OnBackspace(object sender, ExecutedRoutedEventArgs e)
        {
            if (_handlingInput)
                return;
            _handlingInput = true;
            Status = CodeControlStatus.InputHandling;
            var b = await DoInputAsync(new InputRequest(InputRequestKind.Backspace)).ConfigureAwait(true);
            if (!b.IsSuccess)
#if DEBUG
                _debugFn?.Invoke("Backspace failed");
#endif
            _handlingInput = false;
            Status = CodeControlStatus.Idle;
        }

        #endregion

        private static void SetupCommands(RoslynCodeControl control1, UIElement control)
        {
            control.CommandBindings.Add(new CommandBinding(EditingCommands.EnterLineBreak, control1.OnEnterLineBreak,
                control1.CanEnterLineBreak));
            control.CommandBindings.Add(new CommandBinding(EditingCommands.Backspace, control1.OnBackspace,
                control1.CanExecuteBackspace));
            control.CommandBindings.Add(new CommandBinding(EditingCommands.MoveRightByCharacter,
                control1.OnMoveRightByCharacter, control1.CanExecuteMoveRightByCharacter));
            control.CommandBindings.Add(new CommandBinding(EditingCommands.MoveLeftByCharacter,
                control1.OnMoveLeftByCharacter, control1.CanExecuteMoveLeftByCharacter));
            control.CommandBindings.Add(new CommandBinding(EditingCommands.MoveUpByLine, control1.OnMoveUpByLine,
                control1.CanExecuteOnMoveUpByLine));
            control.CommandBindings.Add(new CommandBinding(EditingCommands.MoveDownByLine, control1.OnMoveDownByLine,
                control1.CanExecuteOnMoveDownByLine));


            control.InputBindings.Add(new KeyBinding(EditingCommands.EnterLineBreak, Key.Enter, ModifierKeys.None));
            control.InputBindings.Add(new KeyBinding(EditingCommands.Backspace, Key.Back, ModifierKeys.None));
            control.InputBindings.Add(
                new KeyBinding(EditingCommands.MoveRightByCharacter, Key.Right, ModifierKeys.None));
            control.InputBindings.Add(new KeyBinding(EditingCommands.MoveLeftByCharacter, Key.Left, ModifierKeys.None));
            control.InputBindings.Add(new KeyBinding(EditingCommands.MoveUpByLine, Key.Up, ModifierKeys.None));
            control.InputBindings.Add(new KeyBinding(EditingCommands.MoveDownByLine, Key.Down, ModifierKeys.None));
        }

        private async void OnEnterLineBreak(object sender, ExecutedRoutedEventArgs e)
        {
            if (_handlingInput)
                return;
            _handlingInput = true;
            Status = CodeControlStatus.InputHandling;
            var b = await DoInputAsync(new InputRequest(InputRequestKind.NewLine)).ConfigureAwait(true);
            if (!b.IsSuccess)
#if DEBUG
                _debugFn?.Invoke("Newline failed");
#endif
            _handlingInput = false;
            Status = CodeControlStatus.Idle;
        }

        private void CanEnterLineBreak(object sender, CanExecuteRoutedEventArgs e)
        {
            if (_handlingInput) return;
            e.CanExecute = true;
            e.Handled = true;
        }


        private void OnSyntaxTreeUpdated(SyntaxTree newValue)
        {
            if (!UpdatingSourceText || newValue == null) return;
#if SYNCSOURCETEXT
            _text = newValue.GetText();
            SourceText = _text.ToString();
#endif
        }

        /// <summary>
        /// 
        /// </summary>
        /// <exception cref="InvalidOperationException"></exception>
        public async Task UpdateTextSourceAsync()
        {
            // if (!UiLoaded)
            // return;
            if (Compilation != null && Compilation.SyntaxTrees.Contains(SyntaxTree) == false)
                throw new InvalidOperationException();

            if (SyntaxNode == null || SyntaxTree == null) return;
            if (ReferenceEquals(SyntaxNode.SyntaxTree, SyntaxTree) == false)
                throw new InvalidOperationException("SyntaxNode is not within syntax tree");

            await UpdateFormattedTextAsync();
        }


        #region Debug Elements

#if DEBUG
        public UIElement Debug3Container { get; set; }

        public UIElement Debug1Container { get; set; }
        public UIElement Debug2Container { get; set; }
#endif

        #endregion

        #region thread

        public static Thread StartSecondaryThread(ManualResetEvent mEvent = default,
            Action<object> cb = null)
        {
            var t = new ParameterizedThreadStart(SecondaryThreadStart);
            var newWindowThread = SecondaryThread = new Thread(t);
            newWindowThread.SetApartmentState(ApartmentState.STA);
            newWindowThread.Name = "SecondaryThread";
            newWindowThread.IsBackground = true;
            newWindowThread.Start(mEvent);
            return newWindowThread;
        }

        public static Thread SecondaryThread { get; set; }

        private static void SecondaryThreadStart(object o)
        {
            Debug.WriteLine("Started secondary thread");
            var mr = (ManualResetEvent) o;

            var d = Dispatcher.CurrentDispatcher;

            StaticSecondaryDispatcher = d;

            if (mr != null) mr.Set();
            Dispatcher.Run();
            Debug.WriteLine("Exiting Secondary Thread");
            StaticSecondaryDispatcher = null;
            SecondaryThread = null;
        }

        public static Dispatcher StaticSecondaryDispatcher { get; set; }

        #endregion

        public TranslateTransform Translate { get; set; }


        public async Task<UpdateComplete> DoInputAsync(InputRequest inputRequest)
        {
            if (IsFaulted) throw new CodeControlFaultException("Unknown Fault", Exception1);

            var insertionPoint = InsertionPoint;
            var complete = await DoUpdateTextAsync(insertionPoint, inputRequest).ConfigureAwait(true);
#if true
            DebugFn("About to update roslyn properties");
            if (CustomTextSource != null)
            {
                ChangingText = true;
                SyntaxNode = CustomTextSource.Node;
                SyntaxTree = CustomTextSource.Tree;
                var doc = Document.WithSyntaxRoot(SyntaxNode);
                var sm = await doc.GetSemanticModelAsync();
                if (sm != null)
                {
                    Compilation = sm.Compilation;
                }

                Document = doc;
                SemanticModel = sm;
                ChangingText = false;
                DebugFn("Finished updating roslyn properties.");
            }
            else
            {
                DebugFn("Text source is null");
            }

            InsertionPoint = complete.NewInsertionPoint;
#endif

            return complete;
        }


        private async Task<UpdateComplete> DoUpdateTextAsync(int insertionPoint, InputRequest inputRequest)
        {
            DebugFn($"DoUpdateTextAsync [{insertionPoint}] {inputRequest}");

            var insLine = InsertionLine;
            var insertionLineOffset = insLine?.Offset ?? 0;
            var originY = insLine?.Origin.Y ?? 0;
            var originX = insLine?.Origin.X ?? 0;
            var insertionLineLineNumber = insLine?.LineNumber ?? 0;
            inputRequest.SequenceId = SequenceId++;
            var renderRequestInput = new RenderRequestInput(this,
                insertionLineLineNumber,
                insertionLineOffset,
                originY, originX,
                Formatter,
                OutputWidth,
                PixelsPerDip,
                CustomTextSource,
                MaxY, MaxX, FontSize, _typefaceName, FontWeight);
            var renderRequest = new RenderRequest(inputRequest, insertionPoint, renderRequestInput, insLine);
            await RenderChannel.Writer.WriteAsync(renderRequest);
            var updateComplete = await UpdateCompleteChannel.Reader.ReadAsync();
#if DEBUG
            DebugFn($"{nameof(DoUpdateTextAsync)} Update complete: {updateComplete}");
#endif
            return updateComplete;
        }

        public int SequenceId { get; set; } = 1;


        #region Focus

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

        #endregion

        private static RedrawLineResult RedrawLine(RenderRequestInput renderRequestInput,
            FontRendering currentRendering, TextChange? change, LineInfo2 curLineInfo)
        {
            var begin = DateTime.Now;
            Action<string> debugFn = renderRequestInput.RoslynCodeControl.DebugFn;
#if DEBUG
            debugFn(nameof(RedrawLine));
#endif
            var lineNo = renderRequestInput.LineNo;
            var lineOriginPoint = new Point(renderRequestInput.X, renderRequestInput.Y);

            double width, height;
            var dg = new DrawingGroup();
            var dc = dg.Open();
            LineInfo2 lineInfo2;
            var runsInfos = new List<TextRunInfo>();
            var source = renderRequestInput.CustomTextSource4;
            var runCount = source.Runs?.Count(ri => true) ?? 0;
            if (runCount == 0)
            {
#if DEBUG
                debugFn("Run count is 0");
#endif
            }
            else
            {
            }

            LinkedList<CharInfo> allCharInfos;
            allCharInfos = change.HasValue && curLineInfo?.FirstCharInfo != null
                ? curLineInfo.FirstCharInfo.List
                : new LinkedList<CharInfo>();
            var newLineInfo = false;

            using (var myTextLine = renderRequestInput.TextFormatter.FormatLine(source,
                renderRequestInput.Offset, renderRequestInput.ParagraphWidth,
                new GenericTextParagraphProperties(currentRendering,
                    renderRequestInput.PixelsPerDip), null))
            {
#if DEBUG
                debugFn("got a text line " + myTextLine.Length);
#endif
                var textStorePosition = renderRequestInput.Offset;
                // ReSharper disable once PossibleNullReferenceException
                var nRuns = source.Runs.Count - runCount;

                CommonText.HandleLine(allCharInfos, lineOriginPoint, myTextLine, source, runCount,
                    nRuns, lineNo, textStorePosition, runsInfos, debugFn, change, curLineInfo);

                myTextLine.Draw(dc, lineOriginPoint, InvertAxes.None);

                width = myTextLine.Width;
                height = myTextLine.Height;

                if (curLineInfo != null)
                {
                    lineInfo2 = curLineInfo;
                    lineInfo2.Length = myTextLine.Length;
                    lineInfo2.Height = myTextLine.Height;
                }
                else
                {
                    lineInfo2 = new LineInfo2(renderRequestInput.LineNo, allCharInfos.First, textStorePosition,
                        lineOriginPoint, myTextLine.Height, myTextLine.Length);
                    newLineInfo = true;
                }
            }

            dc.Close();
#if DEBUG
            debugFn("Complete");
#endif
            return new RedrawLineResult(lineInfo2, dg, lineOriginPoint.X + width, lineOriginPoint.Y + height,
                allCharInfos, runsInfos, newLineInfo, begin);
        }

        private static void PostUpdate(PostUpdateInput @in, int newIp)
        {
            var roslynCodeControl1 = @in.RoslynCodeControl;
            roslynCodeControl1.DebugFn("entering PostUpdate routine");

            var res = @in.RedrawLineResult;
            var inDg = res.DrawingGroup;

            if ( inDg.Bounds.IsEmpty && res.LineInfo.Length > 2) throw new InvalidOperationException("Drawing group has empty bounds");
            roslynCodeControl1.DebugFn($"Drawing Group bounds is {inDg.Bounds}");
            roslynCodeControl1.DebugFn($"Line info is {res.LineInfo}");
            var inMaxX = res.LineMaxX;
            var inMaxY = res.LineMaxY;

            var textDest = roslynCodeControl1.TextDestination;
            var lineNo = res.LineInfo.LineNumber;

#if GROUPEDDG
            var i = lineNo / 100;
            var j = lineNo % 100;
            if (textDest.Children.Count <= i)
            {
                var drawingGroup = new DrawingGroup();
                for (var k = 0; k < j; k++) drawingGroup.Children.Add(new DrawingGroup());
                drawingGroup.Children.Add(inDg);
                textDest.Children.Add(drawingGroup);
            }
            else
            {
                var drawingGroup = (DrawingGroup) textDest.Children[i];
                for (var k = 0; k < j; k++) drawingGroup.Children.Add(new DrawingGroup());

                if (j >= drawingGroup.Children.Count)
                    drawingGroup.Children.Add(inDg);
                else
                    drawingGroup.Children[j] = inDg;
            }
#else
            var i = lineNo;
            if (textDest.Children.Count <= i)
            {
                while (textDest.Children.Count < i - 1)
                    textDest.Children.Add(new GeometryDrawing());
                textDest.Children.Add(inDg);
            }
            else
            {
                textDest.Children[i] = inDg;
            }
#endif


            var maxX = Math.Max(roslynCodeControl1.MaxX, inMaxX);
            roslynCodeControl1.MaxX = maxX;
            var maxY = Math.Max(roslynCodeControl1.MaxY, inMaxY);
            roslynCodeControl1.MaxY = maxY;
            // bound to viewbox height / width
            // roslynCodeControl.Rectangle.Width = lineCtxMaxX;
            // roslynCodeControl.Rectangle.Height = lineCtxMaxY;
            if (roslynCodeControl1._rect2 != null)
            {
                roslynCodeControl1._rect2.Width = maxX;
                roslynCodeControl1._rect2.Height = maxY;
            }

            var boundsLeft = Math.Min(roslynCodeControl1.TextDestination.Bounds.Left, 0);
            boundsLeft -= 3;
            var boundsTop = Math.Min(roslynCodeControl1.TextDestination.Bounds.Top, 0);
            boundsTop -= 3;

            var width = maxX - boundsLeft;
            var height = maxY - boundsTop;
            roslynCodeControl1.DrawingBrush.Viewbox = roslynCodeControl1.DrawingBrushViewbox =
                new Rect(boundsLeft, boundsTop, width, height);

            roslynCodeControl1.Rectangle.Width = width;
            roslynCodeControl1.Rectangle.Height = height;
            
            LinkedListNode<LineInfo2> llNode = null;
            var setInsertionLineNode = false;
            if (@in.RedrawLineResult.IsNewLineInfo)
            {
                setInsertionLineNode = true;
                var li0 = roslynCodeControl1.FindLine(res.LineInfo.LineNumber, roslynCodeControl1.InsertionLineNode);
                if (li0 == null)
                {
                    li0 = roslynCodeControl1.FindLine(res.LineInfo.LineNumber - 1);
                    if (li0 != null)
                    {
                        llNode = roslynCodeControl1.LineInfos2.AddAfter(li0, res.LineInfo);
                    }
                    else
                    {
                        if (roslynCodeControl1.LineInfos2.Any()) throw new InvalidOperationException();
                        llNode = roslynCodeControl1.LineInfos2.AddFirst(res.LineInfo);
                        roslynCodeControl1.OnPropertyChanged(nameof(FirstLine));
                    }
                }
                else
                {
                    // if (Equals(roslynCodeControl1.LineInfos2.First, li0))
                    // roslynCodeControl1.OnPropertyChanged(nameof(FirstLine));
                    li0.Value = res.LineInfo;
                    // roslynCodeControl1.OnPropertyChanged(nameof(InsertionLine));
                    llNode = li0;
                }
            }

            var nextLineOffset = res.LineInfo.Offset + res.LineInfo.Length;

            if (newIp == nextLineOffset)
            {
                if (!@in.RedrawLineResult.IsNewLineInfo)
                    llNode = roslynCodeControl1.FindLine(res.LineInfo.LineNumber, roslynCodeControl1.InsertionLineNode);
                // ReSharper disable once PossibleNullReferenceException
                // ReSharper disable once PossibleNullReferenceException
                llNode = llNode.List.AddAfter(llNode,
                    new LineInfo2(res.LineInfo.LineNumber + 1, null, nextLineOffset,
                        new Point(res.LineInfo.Origin.X, res.LineInfo.Origin.Y + res.LineInfo.Height), 0, 0));
                setInsertionLineNode = true;
            }

            if (setInsertionLineNode)
                roslynCodeControl1.InsertionLineNode = llNode;
            
            roslynCodeControl1.DebugFn("return");
        }


        /// <summary>
        /// 
        /// </summary>
        // ReSharper disable once MemberCanBePrivate.Global
        public static TextFormatter Formatter { get; } = CommonText.Formatter;

#if false
        protected override Size ArrangeOverride(Size arrangeBounds)
        {
            _nliens = (int) (arrangeBounds.Height / (FontFamily.LineSpacing * FontSize));

            var arrangeOverride = base.ArrangeOverride(arrangeBounds);
            var scrollBar = (ScrollBar) _scrollViewer?.Template.FindName("PART_VerticalScrollBar", _scrollViewer);

            OutputWidth = _scrollViewer?.ActualWidth - scrollBar?.ActualWidth - Rectangle?.StrokeThickness * 2 ?? 0.0;
            if (InitialUpdate)
            {
                if (PerformingUpdate)
                {
                    DebugFn("already performing update");
                    return arrangeOverride;
                }

                InitialUpdate = false;
                if (SyntaxNode == null)
                    return arrangeBounds;
                DebugFn("Performing initial update of text");
                var updateFormattedText = CommonText.UpdateFormattedText(this);
                UpdateFormattedTestTask = updateFormattedText;
            }

            return arrangeOverride;
        }
#endif

        private void OnFilenameChanged(string oldValue, string newValue)
        {
            if (newValue == null)
                return;

            Status = CodeControlStatus.Reading;

            using (var sr = File.OpenText(newValue))
            {
                var code = sr.ReadToEnd();


                Status = CodeControlStatus.Idle;
                SourceText = code;
            }
        }

        private void UpdateCaretPosition(int? oldValue = null, int? newValue = null)
        {
            try
            {
                var charInfoNode = InsertionCharInfoNode;
                if (charInfoNode == null)
                {
#if DEBUG
                    _debugFn?.Invoke($"{nameof(UpdateCaretPosition)}  {nameof(InsertionCharInfoNode)} is null.");
#endif
                    if (InsertionLine != null) charInfoNode = InsertionLine.FirstCharInfo;
                }

                var f = charInfoNode == null || charInfoNode.Value.Index < newValue;
                LinkedListNode<CharInfo> prevCharInfoNode = null;
                while (charInfoNode != null &&
                       (f ? charInfoNode.Value.Index < newValue : charInfoNode.Value.Index > newValue))
                {
                    prevCharInfoNode = charInfoNode;
                    charInfoNode = f ? charInfoNode.Next : charInfoNode.Previous;
                }

                if (charInfoNode == null)
                {
                    if (prevCharInfoNode != null)
                    {
                        var ci = prevCharInfoNode.Value;
                        var ciYOrigin = ci.YOrigin - DrawingBrush.Viewbox.Top;
                        _textCaret.SetValue(Canvas.TopProperty, ciYOrigin);
                        var ciAdvanceWidth = ci.XOrigin + ci.AdvanceWidth - DrawingBrush.Viewbox.Left;
                        _textCaret.SetValue(Canvas.LeftProperty, ciAdvanceWidth);
                        DebugFn($"Caret1 - {ciAdvanceWidth}x{ciYOrigin}");
                        return;
                    }
                    else
                    {
                        // ReSharper disable once PossibleNullReferenceException
                        var ciYOrigin = InsertionLine.Origin.Y - DrawingBrush.Viewbox.Top;
                        _textCaret.SetValue(Canvas.TopProperty, ciYOrigin);
                        var ciAdvanceWidth = -1 * DrawingBrush.Viewbox.Left;
                        _textCaret.SetValue(Canvas.LeftProperty, ciAdvanceWidth);

                        DebugFn($"Caret2 - {ciAdvanceWidth}x{ciYOrigin}");

                        MaxY = Math.Max(MaxY, ciYOrigin - DrawingBrush.Viewbox.Top + _textCaret.lineHeight);
                        Rectangle.Height = MaxY;
                        DrawingBrush.Viewbox = DrawingBrushViewbox = new Rect(DrawingBrushViewbox.X,
                            DrawingBrushViewbox.Y,
                            DrawingBrushViewbox.Width, MaxY);
                        return;
                    }
                }
                else
                {
                    if (prevCharInfoNode != null)
                    {
                        var ci = prevCharInfoNode.Value;
                        var ciYOrigin = ci.YOrigin - DrawingBrush.Viewbox.Top;
                        _textCaret.SetValue(Canvas.TopProperty, ciYOrigin);
                        var ciAdvanceWidth = ci.XOrigin + ci.AdvanceWidth - DrawingBrush.Viewbox.Left;
                        _textCaret.SetValue(Canvas.LeftProperty,
                            ciAdvanceWidth);

                        DebugFn($"Caret3 - {ciAdvanceWidth}x{ciYOrigin}");
                        return;
                    }
                }

                DebugFn("no position");
            }
            catch
            {
                // ignored
            }
#if false
            DebugFn($"{nameof(UpdateCaretPosition)} ( {oldValue} , {newValue} )");

            var insertionPoint = newValue ?? InsertionPoint;
            var forward = true;
            if (oldValue.HasValue && newValue.HasValue) forward = newValue.Value > oldValue.Value;

            DebugFn($"forward = {forward}");
            int ciIndex;
            if (forward)
                ciIndex = CharInfos.FindIndex(ci0 =>
                    ci0.Index >= insertionPoint);
            else
                ciIndex = CharInfos.FindLastIndex(ci0 =>
                    ci0.Index <= insertionPoint);

            DebugFn($"Found index {ciIndex}");
            CharInfo ci = null;
            if (forward && ciIndex >= 1)
                ciIndex--;
            else if (forward && ciIndex == -1)
                ciIndex = CharInfos.Count - 1;
            else if (forward) ciIndex++;

            if (ciIndex == -1) ciIndex = 0;

            ci = CharInfos[ciIndex];

            DebugFn($"Character is {ci.Character}");
            if (forward)
            {
                if (ci.Index != insertionPoint - 1)
                {
                    ci = CharInfos[ciIndex + 1];
                    _textCaret.SetValue(Canvas.TopProperty, ci.YOrigin - _myDrawingBrush.Viewbox.Top);
                    _textCaret.SetValue(Canvas.LeftProperty, ci.XOrigin - _myDrawingBrush.Viewbox.Left);
                    _updatingCaret = true;
                    InsertionPoint = ci.Index;
                    _updatingCaret = false;
                    InsertionCharInfo = ci;
                }
                else
                {
                    _textCaret.SetValue(Canvas.TopProperty, ci.YOrigin - _myDrawingBrush.Viewbox.Top);
                    _textCaret.SetValue(Canvas.LeftProperty,
                        ci.XOrigin + ci.AdvanceWidth - _myDrawingBrush.Viewbox.Left);
                    if (CharInfos.Count > ciIndex + 1 && CharInfos[ciIndex + 1].Index == ci.Index + 1)
                        InsertionCharInfo = CharInfos[ciIndex + 1];
                    else
                        InsertionCharInfo = null;
                }
            }
            else
            {
                if (ci.Index != insertionPoint)
                {
                    _textCaret.SetValue(Canvas.TopProperty, ci.YOrigin - _myDrawingBrush.Viewbox.Top);
                    _textCaret.SetValue(Canvas.LeftProperty,
                        ci.XOrigin + ci.AdvanceWidth - _myDrawingBrush.Viewbox.Left);
                    _updatingCaret = true;
                    InsertionPoint = ci.Index + 1;
                    _updatingCaret = false;
                    InsertionCharInfo = null;
                }
                else
                {
                    var ciYOrigin = ci.YOrigin;

                    _textCaret.SetValue(Canvas.TopProperty, ciYOrigin - _myDrawingBrush.Viewbox.Top);
                    var leftValue = ci.XOrigin - _myDrawingBrush.Viewbox.Left;
                    DebugFn($"New caret position is ( {leftValue} , {ciYOrigin} )");
                    _textCaret.SetValue(Canvas.LeftProperty, leftValue);
                    InsertionCharInfo = ci;
                }
            }
#endif
        }


        // ReSharper disable once NotAccessedField.Local
        private Border _border;

        // ReSharper disable once NotAccessedField.Local
        private Grid _grid;
        private string _typefaceName;
        private JoinableTaskFactory _jtf2;
        private readonly bool _enableMouse = true;
        private readonly bool _doBinding = false;
        private Task _renderChannelReaderTask;
        private UpdateComplete _lastInputResult;
        private JoinableTask _postUpdateReaderJoinableTask;
        private Task _postUpdateReaderFaultContinuation;
        private JoinableTaskFactory _myJoinableTaskFactory;
        private JoinableTaskContext _joinableTaskContext;
        private JoinableTaskCollection _taskCollection;
        private TypeInfo _typeInfo;
        private AdhocWorkspace _workspace;

        #region Mouse

        /// <inheritdoc />
#if MOUSE
        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (!_enableMouse) return;
            DrawingContext dc = null;
            try
            {
                var point = e.GetPosition(Rectangle);
                if (CustomTextSource?.RunInfos != null)
                {
                    var runInfo = CustomTextSource.RunInfos.Where(zz1 => zz1.Rect.Contains(point)).ToList();
                    if (runInfo.Any())
                    {
#if DEBUG
                        _debugFn?.Invoke(runInfo.Count().ToString());
#endif
                        var first = runInfo.First();
#if DEBUG
                        _debugFn?.Invoke(first.Rect.ToString());
#endif
                        if (first.TextRun == null) return;
#if DEBUG
                        _debugFn?.Invoke(first.TextRun.ToString() ?? "");
#endif
                        if (first.TextRun is CustomTextCharacters c0)
                        {
#if DEBUG
                            _debugFn?.Invoke(c0.Text);
#endif
                        }

                        // fake out hover region info
                        HoverRegionInfo = new RegionInfo(first.TextRun, first.Rect, new List<CharacterCell>());
                    }
                }

                return;
#if false
                var q = LineInfos.SkipWhile(z => z.Origin.Y < point.Y);
                if (q.Any())
                {
                    var line = q.First();
                    // DebugFn(line.LineNumber.ToString());
                    if (line.Regions != null)
                    {
                        var qq = line.Regions.SkipWhile(zz0 => !zz0.BoundingRect.Contains(point));
                        if (qq.Any())
                        {
                            var region = qq.First();
#if DEBUG
                            _debugFn?.Invoke(region.SyntaxToken?.ToString());
#endif
                        }
                    }
                }

                var zz = LineInfos.Where(z => z.Regions != null).SelectMany(z => z.Regions)
                    .Where(x => x.BoundingRect.Contains(point)).ToList();
                if (zz.Count > 1)
#if DEBUG
                    _debugFn?.Invoke("Multiple regions matched");
#endif
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
                // DebugFn(g.Item2.SyntaxNode?.Kind().ToString() ?? "");
                // DebugFn(((RectangleGeometry)g).Rect);

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
                    // if (tuple.Trivia.HasValue) DebugFn(tuple
                    // ~.ToString());

                    if (tuple.SyntaxNode != HoverSyntaxNode)
                    {
                        if (ToolTip is ToolTip tt) tt.IsOpen = false;
                        HoverSyntaxNode = tuple.SyntaxNode;
                        if (tuple.SyntaxNode != null)
                        {
                            ISymbol sym = null;
                            IOperation operation = null;
                            if (SemanticModel != null)
                                try
                                {
                                    sym = SemanticModel?.GetDeclaredSymbol(tuple.SyntaxNode);
                                    operation = SemanticModel.GetOperation(tuple.SyntaxNode);
                                    // var zzz = tuple.SyntaxNode.AncestorsAndSelf().OfType<ForEachStatementSyntax>()
                                    // .FirstOrDefault();
                                    // if (zzz != null)
                                    // {
                                    // var info = Model.GetForEachStatementInfo(zzz);
                                    // DebugFn(info.ElementType?.ToDisplayString());
                                    // }

                                    // switch ((CSharpSyntaxNode) tuple.SyntaxNode)
                                    // {
                                    // case AssignmentExpressionSyntax assignmentExpressionSyntax:
                                    // break;
                                    // case ForEachStatementSyntax forEachStatementSyntax:
                                    // var info = Model.GetForEachStatementInfo(forEachStatementSyntax);
                                    // DebugFn(info.ElementType.ToDisplayString());
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
#if DEBUG
                                _debugFn?.Invoke(sym.Kind.ToString());
#endif
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
                        // DebugFn("out of bounds");
                        // }
                        // else
                        // {
                        // var chars = _chars[item2Y];
                        // DebugFn("y is " + item2Y, DebugCategory.MouseEvents);
                        // var item2X = (int) item2.X;
                        // if (item2X >= chars.Count)
                        // {
                        //DebugFn("out of bounds");
                        // }
                        // else
                        // {
                        // var ch = chars[item2X];
                        // DebugFn("Cell is " + item2 + " " + ch, DebugCategory.MouseEvents);
                        var newOffset = tuple.Offset + cellIndex;
                        HoverOffset = newOffset;
                        HoverColumn = (int) item2.X;
                        HoverRow = (int) item2.Y;
                        if (SelectionEnabled && IsSelecting)
                        {
                            if (_selectionGeometry != null) TextDestination.Children.Remove(_selectionGeometry);
#if DEBUG
                            _debugFn?.Invoke("Calculating selection");
#endif

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
#if DEBUG
                                _debugFn?.Invoke($"Region offset {regionInfo.Offset} : Length {regionInfo.Length}");
#endif

                                if (regionInfo.Offset <= begin)
                                {
                                    var takeNum = begin - regionInfo.Offset;
#if DEBUG
                                    _debugFn?.Invoke("Taking " + takeNum);
#endif
                                    foreach (var tuple1 in regionInfo.Characters.Take(takeNum))
                                    {
#if DEBUG
                                        _debugFn?.Invoke("Adding " + tuple1);
#endif
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
                            // _myDrawingBrush.Drawing = TextDestination;
                            _selectionEnd = newOffset;
                            // InvalidateVisual();
                        }
                    }

                    var textRunProperties = tuple.TextRun.Properties;
                    if (!(textRunProperties is GenericTextRunProperties)) continue;
                    if (_rect != tuple.BoundingRect)
                    {
                        _rect = tuple.BoundingRect;
                        // if (_geometryDrawing != null) TextDestination.Children.Remove(_geometryDrawing);


                        var solidColorBrush = new SolidColorBrush(Colors.CadetBlue) {Opacity = .6};


                        // _dg2.Children.Add(_geometryDrawing);
                        // InvalidateVisual();
                    }

                    //DebugFn(pp.Text);
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
#endif
            }
            catch (Exception ex)
            {
#if DEBUG
                _debugFn?.Invoke(ex.ToString());
#endif
            }
            finally
            {
                dc?.Close();
            }
        }

        /// <inheritdoc />
        protected override void OnMouseUp(MouseButtonEventArgs e)
        {
            if (!_enableMouse)
                return;
            if (IsSelecting)
            {
                IsSelecting = false;
                _endNode = HoverSyntaxNode;
#if DEBUG
                _debugFn?.Invoke($"{_startOffset} {_selectionEnd}");
#endif
                if (_startNode != null)
                    if (_endNode != null)
                    {
                        var st1 = _startNode.AncestorsAndSelf().OfType<StatementSyntax>().FirstOrDefault();
                        var st2 = _endNode.AncestorsAndSelf().OfType<StatementSyntax>().FirstOrDefault();
                        if (st1 != null)
                            if (st2 != null)
                                if (SemanticModel != null)
                                {
                                    var r = SemanticModel.AnalyzeDataFlow(st1, st2);
                                    if (r != null)
                                        return;
#if DEBUG
                                    _debugFn?.Invoke((string) (r != null && r.Succeeded).ToString());
#endif
                                }
                    }

                Rectangle.ReleaseMouseCapture();
            }
        }

        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            InsertionPoint = HoverOffset;
        }
#endif

        #endregion

        public bool SelectionEnabled { get; set; }

        public bool IsSelecting { get; set; }

        public AdhocWorkspace Workspace
        {
            get { return _workspace; }
            set
            {
                if (Equals(value, _workspace)) return;
                _workspace = value;
                OnPropertyChanged();
            }
        }

        private static void OnNodeUpdated(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var ss = (RoslynCodeControl) d;
            ss.OnNodeUpdated();
        }

        /// <summary>
        /// 
        /// </summary>
        private void OnNodeUpdated()
        {
            if (ChangingText || UpdatingSourceText) return;
#if DEBUG
            _debugFn?.Invoke("SyntaxNode updated");
#endif
            // LineInfos.Clear();
            MaxX = 0;
            MaxY = 0;
            _scrollViewer?.ScrollToTop();
            // if (SecondaryDispatcher != null)
            // await UpdateTextSourceAsync();

            //UpdateFormattedText();
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
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public async Task Shutdown()
        {
            PostUpdateChannel.Writer.Complete();
            RenderChannel.Writer.Complete();
            UpdateCompleteChannel.Writer.Complete();
            UpdateChannel.Writer.Complete();
            foreach (var joinableTask in _taskCollection) DebugFn("Task " + joinableTask.ToString());
            await _taskCollection.JoinTillEmptyAsync();
            DebugFn("return from shutdown");
        }
    }

    public class CodeControlFaultException : Exception
    {
        /// <inheritdoc />
        public CodeControlFaultException()
        {
        }

        /// <inheritdoc />
        protected CodeControlFaultException([NotNull] SerializationInfo info, StreamingContext context) : base(info,
            context)
        {
        }

        /// <inheritdoc />
        public CodeControlFaultException([CanBeNull] string message) : base(message)
        {
        }

        /// <inheritdoc />
        public CodeControlFaultException([CanBeNull] string message, [CanBeNull] Exception innerException) : base(
            message, innerException)
        {
        }
    }
}