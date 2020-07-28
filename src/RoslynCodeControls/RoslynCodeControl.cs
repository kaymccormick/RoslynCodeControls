#define MOUSE
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
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
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.QuickInfo;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Threading;
using CSharpExtensions = Microsoft.CodeAnalysis.CSharp.CSharpExtensions;
using TextChange = Microsoft.CodeAnalysis.Text.TextChange;
// ReSharper disable UnusedParameter.Global
#pragma warning disable 8629
#pragma warning disable 8625
#pragma warning disable 8604
#pragma warning disable 8602
#pragma warning disable 8601
#pragma warning disable 8622
#pragma warning disable 8618
#pragma warning disable 8600

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
    public sealed class RoslynCodeControl : RoslynCodeBase, INotifyPropertyChanged
    {
        public delegate void ContentChangedRoutedEventHandler(
            object sender,
            ContentChangedRoutedEventArgs e);

        public static readonly RoutedEvent ContentChangedEvent = EventManager.RegisterRoutedEvent("ContentChanged",
            RoutingStrategy.Bubble, typeof(ContentChangedRoutedEventHandler), typeof(RoslynCodeControl));

        /// <inheritinpc />
        public RoslynCodeControl() : this(null)
        {
        }

        public RoslynCodeControl(DebugDelegate debugOut = null) : base(debugOut)
        {
            XOffset = 80;
            OutputWidth = 0;
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
            UpdateCompleteChannel = Channel.CreateBounded<UpdateComplete>(new BoundedChannelOptions(1)
                {SingleReader = true, SingleWriter = true});
            Rectangle = new Rectangle();
            if (_doBinding)
            {
                Rectangle.SetBinding(WidthProperty,
                    new Binding("DrawingBrushViewbox.Width") {Source = this});
                Rectangle.SetBinding(HeightProperty,
                    new Binding("DrawingBrushViewbox.Height") {Source = this});
            }

            RenderChannel = Channel.CreateBounded<RenderRequest>(new BoundedChannelOptions(1)
                {SingleReader = true, SingleWriter = true});
            PostUpdateChannel = Channel.CreateBounded<PostUpdateRequest>(new BoundedChannelOptions(1)
                {SingleReader = true, SingleWriter = true});

            UpdateChannel = Channel.CreateBounded<UpdateInfo>(new BoundedChannelOptions(1)
                {SingleReader = true, SingleWriter = true});

            var joinableTaskContext = new JoinableTaskContext(Dispatcher.Thread, new DispatcherSynchronizationContext(Dispatcher));
            _taskCollection = joinableTaskContext.CreateCollection();
            _myJoinableTaskFactory = joinableTaskContext.CreateFactory(_taskCollection);


            var postUpdateReaderJoinableTask = _myJoinableTaskFactory.RunAsync(StartPostUpdateReaderAsync);

            var taskScheduler = TaskScheduler.FromCurrentSynchronizationContext();
            ControlTaskScheduler = taskScheduler;
            _postUpdateReaderFaultContinuation = postUpdateReaderJoinableTask.Task.ContinueWith(FaultHandler, null,
                CancellationToken.None, TaskContinuationOptions.OnlyOnFaulted, taskScheduler);

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
                    ? insertionPoint - @in.Change.Value.Span.Length
                    : insertionPoint + (text?.Length ?? 0);

                PostUpdate(@in, ip);
#if DEBUG
                DebugFn("Writing update complete " + @in);
#endif
                var updateComplete = new UpdateComplete(@in.InputRequest, ip,
                    @in.Change,
                    @in.Timestamp, r.RenderRequestTimestamp,
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
            new PropertyMetadata(default(SyntaxNode), OnHoverSyntaxNodeUpdated));

        public static readonly DependencyProperty HoverRowProperty = DependencyProperty.Register(
            "HoverRow", typeof(int), typeof(RoslynCodeControl), new PropertyMetadata(default(int)));


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
            // ReSharper disable once UnusedMember.Global
            get { return (string) GetValue(FilenameProperty); }
            set { SetValue(FilenameProperty, value); }
        }

        private static void OnFilenameChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((RoslynCodeControl) d).OnFilenameChanged((string) e.OldValue, (string) e.NewValue);
        }


        public LinkedListNode<CharInfo> InsertionCharInfoNode { get; set; }

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
                if (SemanticModel == null) return;
                if (SyntaxNode == null) return;
                var nodeOrToken = SyntaxNode.ChildThatContainsPosition(newValue);
                if (!nodeOrToken.IsNode) return;
                var syntaxNode = nodeOrToken.AsNode();
                SyntaxNodeOrToken x = null;
                while (syntaxNode != null)
                {
                    x = syntaxNode.ChildThatContainsPosition(newValue);
                    syntaxNode = x.AsNode();
                }

                syntaxNode = x.Parent;
                // ReSharper disable once PossibleNullReferenceException
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
                    TypeInfo = default;
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
        #region Private members

        private readonly DrawingBrush _myDrawingBrush;

        // ReSharper disable once NotAccessedField.Local
        private int _startColumn;

        // ReSharper disable once NotAccessedField.Local
        private int _startRow;
        private int _startOffset;

        private Rectangle _rect2;
        // ReSharper disable once NotAccessedField.Local
        private DrawingGroup _dg2;
        // ReSharper disable once NotAccessedField.Local
        private Grid _innerGrid;
        public TextCaret TextCaret { get; private set; }
        private Canvas _canvas;

        private int _selectionEnd;
        private SyntaxNode _startNode;
        private SyntaxNode _endNode;
#pragma warning disable 169
        private SourceText _text;
#pragma warning restore 169
        private readonly ObjectAnimationUsingKeyFrames _x1;
        private ISymbol _enclosingSymbol;
        private bool _handlingInput;
#pragma warning disable 649
        private bool _updatingCaret;
#pragma warning restore 649
        private readonly DrawingGroup _textDestination;

        #endregion

        #region Public properties

        public Channel<UpdateComplete> UpdateCompleteChannel { get; }

        public LineInfo2? FirstLine
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
                    DebugFn("reading render channel");
                    inp = await RenderChannel.Reader.ReadAsync();
                }
                catch (ChannelClosedException)
                {
                    break;
                }

                try
                {
                    await HandleRenderRequestAsync(inp).ContinueWith(HandleFault, CancellationToken.None,
                        TaskContinuationOptions.OnlyOnFaulted, ControlTaskScheduler);
                }
                catch (TaskCanceledException)
                {
                }
            }

            DebugFn("exiting render channel");
            // ReSharper disable once FunctionNeverReturns
        }

        public TaskScheduler ControlTaskScheduler { get; }

        #endregion

        private async Task HandleRenderRequestAsync(RenderRequest renderRequest)
        {
#if DEBUG
            DebugFn($"{nameof(HandleRenderRequestAsync)}: {renderRequest}");
#endif
            var inn = renderRequest.Input;
            // ReSharper disable once ConvertIfStatementToNullCoalescingAssignment
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
                    change = (TextChange?) await inn.CustomTextSource4.TextInputAsync(renderRequest.InsertionPoint,
                        renderRequest.InputRequest, renderRequest.LineInfo?.Offset ?? 0);
                    DebugFn("returned from TextInputAsync");
                }
                catch (Exception ex)
                {
                    DebugFn("exception");
                    throw new CodeControlFaultException("Text source input method failed", ex);
                }

            var redrawLine = RedrawLine(inn,
                renderRequest.Input.CustomTextSource4.CurrentRendering, change,
                renderRequest.LineInfo);

            redrawLine.DrawingGroup.Freeze();
            var postUpdateInput = new PostUpdateInput(this,
                renderRequest.InsertionPoint, renderRequest.InputRequest,
                redrawLine, change);
            var postUpdateRequest = new PostUpdateRequest(postUpdateInput, renderRequest.Timestamp);
            DebugFn("writing post update request");
            await PostUpdateChannel.Writer.WriteAsync(postUpdateRequest);
            DebugFn("Here");
        }

        #endregion

        #region Overrides

        /// <inheritdoc />
        public override Rect DrawingBrushViewbox
        {
            get { return _drawingBrushViewbox; }
            set
            {
                if (value.Equals(_drawingBrushViewbox)) return;
                _drawingBrushViewbox = value;
                DrawingBrush2.Viewbox = _drawingBrushViewbox;
            }
        }

        public override DrawingBrush DrawingBrush
        {
            get { return _myDrawingBrush; }
        }

        /// <inheritdoc />
        protected override void SecondaryThreadTasks()
        {
            DebugFn("SecondaryThreadTasks");
            _renderChannelReaderTask = RenderChannelReaderAsync().ContinueWith(t =>
            {
                if (!t.IsFaulted) return;
                // ReSharper disable once PossibleNullReferenceException
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

#if DEBUG
            HandleDebugKeyDown(e);
#endif
            if (e.Handled) return;
            // ReSharper disable once SwitchStatementMissingSomeEnumCasesNoDefault
            switch (e.Key)
            {
                case Key.PageDown:
                    if (e.KeyboardDevice.Modifiers == ModifierKeys.None)
                    {
                        e.Handled = true;
                        CodeViewportPanel.PageDown();
                        var lineNode = FindLine((int)CodeViewportPanel.VerticalOffset, InsertionLineNode, true);
                        InsertionLineNode = lineNode;
                        InsertionPoint = lineNode.Value.Offset;
                    }
                    break;
                case Key.PageUp:
                    if (e.KeyboardDevice.Modifiers == ModifierKeys.None)
                    {
                        e.Handled = true;
                        ScrollViewer.PageUp();
                        var lineNode = FindLine((int)ScrollViewer.VerticalOffset);
                        InsertionLineNode = lineNode;
                        InsertionPoint = lineNode.Value.Offset;
                    }
                    break;

                case Key.F3:
                    if (InFindOperation)
                    {
                        
                        if (e.KeyboardDevice.Modifiers == ModifierKeys.Shift)
                        {
                            e.Handled = true;
                            FindPrevious();
                        } else if (e.KeyboardDevice.Modifiers == ModifierKeys.None)
                        {
                            e.Handled = true;
                            FindNext();
                        }
                        
                    }
                    break;
                case Key.F:
                    if (Keyboard.Modifiers == ModifierKeys.Control)
                    {
                        e.Handled = true;
                        Find();

                    }
                    break;
                case Key.Escape:
                    if (CompletionPopup.IsOpen)
                    {
                        e.Handled = true;
                        CompletionPopup.IsOpen = false;
                    }

                    // e.Handled = true;
                    // HandleFault(Task.FromException(new CodeControlFaultException("test fault")));
                    break;
                case Key.Space:
                    if (e.KeyboardDevice.Modifiers == ModifierKeys.Control)
                    {
                        e.Handled = true;
                        JTF.RunAsync(() => DoCompletionAsync(null)).Task.ContinueWith(HandleFault,
                            CancellationToken.None,
                            TaskContinuationOptions.OnlyOnFaulted, TaskScheduler.FromCurrentSynchronizationContext());
                    }

                    break;
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
        #region Text Search
        private void Find()
        {
            InFindOperation = true;
            FindStartIndex = InsertionPoint;
            FindPopup.PlacementRectangle = new Rect(0,0,100,20);
            FindPopup.PlacementTarget = this;
            FindPopup.IsOpen = true;
            FindPopup.Focus();
            Keyboard.Focus(FindTextBox);

        }

        private void FindPrevious()
        {
            var lower = FindTextBox.Text.ToLower(CultureInfo.CurrentUICulture);
            var searchTextLength = lower.Length;
            CurrentFindIndex = FoundTextIndex + searchTextLength;
            var index = TextSourceText.ToLower(CultureInfo.CurrentUICulture).LastIndexOf(lower, CurrentFindIndex - searchTextLength);
            if (index == -1)
            {
                FindTextBox.Background = Brushes.Red;
                return;
            }
            FoundTextIndex = index;
            FindTextBox.Background = Brushes.White;
            double? y = null;
            CharInfo? endChar = null;
            LineInfo2? theLine = null;
            var firstChar = FindCharStretch(index, searchTextLength, ref endChar, ref y, ref theLine);

            var rect1 = new Rect(firstChar.XOrigin, firstChar.YOrigin, endChar.XOrigin + endChar.AdvanceWidth - firstChar.XOrigin, theLine.Height);
            rect1.Inflate(ExpandFoundTextRectSize);
            TextSearchInstanceRect = rect1;

            var scrollPos = theLine.LineNumber - ScrollViewer.ViewportHeight / 2;
            if (scrollPos >= 0)
                ScrollViewer.ScrollToVerticalOffset(scrollPos);
        }
        private void FindNext()
        {
            var lower = FindTextBox.Text.ToLower(CultureInfo.CurrentUICulture);
            var searchTextLength = lower.Length;
            CurrentFindIndex = FoundTextIndex + searchTextLength;
            var index = TextSourceText.ToLower(CultureInfo.CurrentUICulture).IndexOf(lower, CurrentFindIndex);
            if (index == -1)
            {
                FindTextBox.Background = Brushes.Red;
                return;
            }
            FoundTextIndex = index;
            FindTextBox.Background = Brushes.White;
            double? y = null;
            CharInfo? firstChar = null;
            CharInfo? endChar = null;
            LineInfo2? theLine = null;
            firstChar = FindCharStretch(index,  searchTextLength, ref endChar, ref y, ref theLine);

            var rect1 = new Rect(firstChar.XOrigin, firstChar.YOrigin, endChar.XOrigin + endChar.AdvanceWidth - firstChar.XOrigin, theLine.Height);
            rect1.Inflate(ExpandFoundTextRectSize);
            TextSearchInstanceRect = rect1;

            var scrollPos = theLine.LineNumber - ScrollViewer.ViewportHeight / 2;
            if(scrollPos >= 0)
                ScrollViewer.ScrollToVerticalOffset(scrollPos);

        }

        private void FindTextBoxOnTextChanged(object sender, TextChangedEventArgs e)
        {
            if (TextSourceText == null) return;
            // ReSharper disable once StringIndexOfIsCultureSpecific.2
            var lower = FindTextBox.Text.ToLower(CultureInfo.CurrentUICulture);
            var searchTextLength = lower.Length;
            var index = TextSourceText.ToLower(CultureInfo.CurrentUICulture).IndexOf(lower, FindStartIndex);
            if (index == -1)
            {
                FindTextBox.Background = Brushes.Red;
                return;
            }

            FoundTextIndex = index;
            FindTextBox.Background = Brushes.White;
            double? y = null;
            CharInfo? firstChar=null;
            CharInfo? endChar = null;
            LineInfo2? theLine=null;
            firstChar = FindCharStretch(index, searchTextLength, ref endChar, ref y, ref theLine);

            var rect1 = new Rect(firstChar.XOrigin, firstChar.YOrigin, endChar.XOrigin + endChar.AdvanceWidth - firstChar.XOrigin, theLine.Height);
            
            rect1.Inflate(ExpandFoundTextRectSize);
            TextSearchInstanceRect = rect1;
            var scrollPos = theLine.LineNumber - ScrollViewer.ViewportHeight / 2;
            if (scrollPos >= 0)
                ScrollViewer.ScrollToVerticalOffset(scrollPos);

        }

        private CharInfo? FindCharStretch(int index,  int searchTextLength, ref CharInfo endChar,
            ref double? y, ref LineInfo2 theLine)
        {
            CharInfo? firstChar=null;
            foreach (var lineInfo2 in LineInfos2)
            {
                var lineInfo2EndOffset = lineInfo2.Offset + lineInfo2.Length;

                if (lineInfo2EndOffset < index)
                    continue;
                var ciNode = lineInfo2.FirstCharInfo;
                while (ciNode != null)
                {
                    if (ciNode.Value.Index == index)
                    {
                        firstChar = ciNode.Value;
                    }

                    if (ciNode.Value.Index == index + searchTextLength - 1)
                    {
                        endChar = ciNode.Value;
                        break;
                    }

                    ciNode = ciNode.Next;
                }

                y = lineInfo2.Origin.Y;
                theLine = lineInfo2;
                break;
            }

            return firstChar;
        }

        public int CurrentFindIndex { get; set; }

        public int FoundTextIndex { get; set; }

        public bool InFindOperation { get; set; }

        public Rect TextSearchInstanceRect
        {
            get { return _textSearchInstanceRect; }
            set
            {
                if (value.Equals(_textSearchInstanceRect)) return;
                _textSearchInstanceRect = value;
                OnPropertyChanged();
            }
        }

        #endregion

        private void HandleDebugKeyDown(KeyEventArgs e)
        {
            int? i = null;
            if (e.KeyboardDevice.Modifiers != ModifierKeys.Control) return;
            if (e.Key == Key.LeftCtrl || e.Key == Key.RightCtrl)
                return;
            if (e.Key >= Key.D0 && e.Key <= Key.D9)
            {
                i = e.Key - Key.D0 - 1;
                if (i == -1)
                    i = 10;
            }

            else if (e.Key >= Key.F1 && e.Key <= Key.F24)
            {
                i = e.Key - Key.F1;
            }
            if (i.HasValue)
            {
                e.Handled = true;

                var container = i.Value < DebugContainers.Capacity ? DebugContainers[i.Value] : null;
                if (container?.Visibility == Visibility.Visible)
                {
                    foreach (var debugContainer in DebugContainers) debugContainer.Visibility = Visibility.Hidden;
                }
                else
                {
                    foreach (var uiElement in DebugContainers.Where((element, i1) => i1 != i.Value))
                        uiElement.Visibility = Visibility.Hidden;

                    if (container != null) container.Visibility = Visibility.Visible;
                }
                e.Handled = true;
                return;
            }
        }

        private void HandleFault(Task obj)
        {
            if (!obj.IsFaulted)
                return;
            var ex = obj.Exception;
            var errPopup = new Popup();
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
                    // ReSharper disable once PossibleNullReferenceException
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
            var kf = new BooleanKeyFrameCollection
            {
                new DiscreteBooleanKeyFrame(true, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(3))),
                new DiscreteBooleanKeyFrame(false)
            };
            var tl = new BooleanAnimationUsingKeyFrames() {KeyFrames = kf, FillBehavior = FillBehavior.HoldEnd};
            errPopup.BeginAnimation(Popup.IsOpenProperty, tl);
        }

        private async Task DoCompletionAsync(char? ch)
        {
            InCompletion = true;
            CompletionText = "";
            var completionService = CompletionService.GetService(Document);
            CompletionBeginOffset = InsertionPoint;
            // ReSharper disable once NotAccessedVariable
            CompletionTrigger completionTrigger = default;
            // ReSharper disable once RedundantAssignment
            if (ch.HasValue) completionTrigger = CompletionTrigger.CreateInsertionTrigger(ch.Value);

            CompletionList results;
            try
            {
                results = await completionService.GetCompletionsAsync(Document, InsertionPoint);
            }
            catch
            {
                return;
            }

#pragma warning disable 219
            var listBx = false;
#pragma warning restore 219
            DumpCompletions(results);
            CompletionComboBox.ItemsSource = results.Items;
            CompletionComboBox.IsDropDownOpen = true;
            CompletionComboBox.IsEditable = true;
            CompletionComboBox.Width = 100;

            CompletionPopup.StaysOpen = true;
            CompletionPopup.PlacementTarget = TextCaret;
            // var left = Canvas.GetLeft(_textCaret);
            // var top = Canvas.GetTop(_textCaret);
            CompletionPopup.PlacementRectangle = new Rect(3, -1 * LineHeight, 100, LineHeight);
            CompletionPopup.Placement = PlacementMode.Bottom;
            CompletionPopup.IsOpen = true;
#if false
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
#endif
        }

        private void DumpCompletions(CompletionList results)
        {
            DebugFn($"Completion results count is {results.Items.Length}", 0);
            var i = 0;
            foreach (var completionItem in results.Items)
            {
                DebugFn($"[{i:D2}] DisplayText: " + completionItem.DisplayText, 0);
                DebugFn($"[{i:D2}] DisplayTextPrefix: " + completionItem.DisplayTextPrefix, 0);
                DebugFn($"[{i:D2}] DisplayTextSuffix: {completionItem.DisplayTextSuffix}", 0);
                DebugFn($"[{i:D2}] FilterText: {completionItem.FilterText}", 0);
                DebugFn($"[{i:D2}] InlineDescription: {completionItem.InlineDescription}", 0);
                i++;
            }
        }

        public bool InCompletion { get; set; }

        public int CompletionBeginOffset { get; set; }


        public override DrawingGroup TextDestination
        {
            get { return _textDestination; }
        }

        /// <inheritdoc />
        protected override Size MeasureOverride(Size constraint)
        {
            var measureOverride = base.MeasureOverride(constraint);
            // var w = ScrollViewer.DesiredSize.Width;
            return measureOverride;
        }

        /// <inheritdoc />
        public override void OnApplyTemplate()
        {
#if DEBUG
            DebugOnApplyTemplate();
#endif
            LineNumbersDrawingGroup = (DrawingGroup) GetTemplateChild("LineNumbers");
            CodeViewportPanel = (CodeViewportPanel) GetTemplateChild("CodeViewportPanel");
            CodeViewportPanel.CodeControl = this;
            SelectionDrawing = (DrawingGroup) GetTemplateChild("SelectionDrawing");
            CompletionPopup = (Popup) GetTemplateChild("CompletionPopup");
            CompletionComboBox = (ComboBox) GetTemplateChild("CompletionComboBox");
            FindPopup = (Popup) GetTemplateChild("FindPopup");
            FindTextBox = (TextBox) GetTemplateChild("FindTextBox");
            ScrollViewer = (ScrollViewer) GetTemplateChild("ScrollViewer");
            if (ScrollViewer != null)
            {
                OutputWidth = ScrollViewer.ActualWidth;
                ScrollViewer.ScrollToVerticalOffset(InitialScrollPosition);
            }


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

                Rectangle.Width = MaxX - DrawingBrushViewbox.Left + Rectangle.StrokeThickness * 2;
                Rectangle.Height = MaxY - DrawingBrushViewbox.Top + Rectangle.StrokeThickness * 2;
                Rectangle.Fill = DrawingBrush;
                DrawingBrush.Viewbox = DrawingBrushViewbox;
            }

            Translate = (TranslateTransform) GetTemplateChild("TranslateTransform");

            _grid = (Grid) GetTemplateChild("Grid");
            _canvas = (Canvas) GetTemplateChild("Canvas");
            _innerGrid = (Grid) GetTemplateChild("InnerGrid");

            TextCaret = (TextCaret) GetTemplateChild("TextCaret");
            if (TextCaret != null)
            {
                TextCaret.LineHeight = FontSize * 1.1;

                // _canvas.Children.Add(TextCaret);
                Canvas.SetLeft(TextCaret, XOffset);
                Canvas.SetTop(TextCaret, 0);
            }

            _border = (Border) GetTemplateChild("Border");

            _rect2 = (Rectangle) GetTemplateChild("Rect2");
            _dg2 = (DrawingGroup) GetTemplateChild("DG2");
            DrawingBrush2 = (DrawingBrush) GetTemplateChild("DrawingBrush2");
            if (DrawingBrush2 != null) DrawingBrush2.Viewbox = DrawingBrushViewbox;
            if (JTF2 == null)
            {
                JTF2 = JTF;
                if (Workspace != null)
                {
                    // if (Workspace is AdhocWorkspace adhocWorkspace)
                    // {
                        // We dont necessaarily need to add a solution, only if there isnt one
                        // adhocWorkspace.AddSolution(SolutionInfo.Create(SolutionId.CreateNewId(), VersionStamp.Create()));
                    // }

                    if (Document == null)
                    {

                        var projectInfo = ProjectInfo.Create(ProjectId.CreateNewId(), VersionStamp.Create(),
                            "Code Project", "code", LanguageNames.CSharp);
                        var newSolution = Workspace.CurrentSolution.AddProject(projectInfo);
                        Workspace.TryApplyChanges(newSolution);
                        // RaiseEvent(new WorkspaceUpdatedEventArgs(Workspace, this));

                        DocumentInfo documentInfo;
                        var filename = Filename;
                        if (filename != null)
                            documentInfo = DocumentInfo.Create(DocumentId.CreateNewId(projectInfo.Id), "Default",
                                null, SourceCodeKind.Regular, new FileTextLoader(filename, Encoding.UTF8), filename);
                        else
                            documentInfo = DocumentInfo.Create(DocumentId.CreateNewId(projectInfo.Id), "Default",
                                null, SourceCodeKind.Regular);

                        newSolution = Workspace.CurrentSolution.AddDocument(documentInfo);
                        Workspace.TryApplyChanges(newSolution);
                        RaiseEvent(new WorkspaceUpdatedEventArgs(Workspace, this));

                        var Project1 = Workspace.CurrentSolution.GetProject(projectInfo.Id);
                        Document = Workspace.CurrentSolution.GetDocument(documentInfo.Id);
                    }
                }
                if (Document == null)
                {
                    SyntaxTree = SyntaxFactory.ParseSyntaxTree("");
                    SyntaxNode = SyntaxTree.GetRoot();
                }

                JTF.RunAsync(async () =>
                {
                    if (Document != null && SyntaxTree == null)
                    {
                        SyntaxTree = await Document.GetSyntaxTreeAsync();
                        SyntaxNode = await SyntaxTree.GetRootAsync();
                    }

                    await UpdateFormattedTextAsync();
                });
            }
        }

        public DrawingGroup LineNumbersDrawingGroup { get; set; }

        public DrawingBrush DrawingBrush2 { get; set; }

        public TextBox FindTextBox
        {
            get { return _findTextBox; }
            set
            {
                if (Equals(value, _findTextBox)) return;
                if (_findTextBox != null) _findTextBox.TextChanged -= FindTextBoxOnTextChanged;
                _findTextBox = value;
                if (_findTextBox != null) _findTextBox.TextChanged += FindTextBoxOnTextChanged;
            }
        }

       

        public int FindStartIndex { get; set; }

        public Popup FindPopup
        {
            get { return _findPopup; }
            set
            {
                if (Equals(value, _findPopup)) return;
                _findPopup = value;
                
            }
        }

        private void DebugOnApplyTemplate()
        {
            DebugContainers.Clear();
            var i = 0;
            for (;;)
            {
                var partName = $"debug{i + 1}container";
                var container = GetTemplateChild(partName) as UIElement;
                if (container == null) break;


                DebugContainers.Add(container);
                i++;
            }
        }

        public List<UIElement> DebugContainers { get; } = new List<UIElement>();

        public DrawingGroup SelectionDrawing { get; set; }

        public ComboBox CompletionComboBox
        {
            get { return _completionComboBox; }
            set
            {
                if (Equals(_completionComboBox, value)) return;
                if (_completionComboBox != null)
                {
                    _completionComboBox.DropDownOpened -= CompletionComboBoxOnDropDownOpened;
                    _completionComboBox.DropDownClosed -= CompletionComboBoxOnDropDownClosed;
                    _completionComboBox.PreviewKeyDown -= CompletionComboBoxOnPreviewKeyDown;
                    _completionComboBox.GotFocus -= CompletionComboBoxOnGotFocus;
                    _completionComboBox.LostFocus -= CompletionComboBoxOnLostFocus;
                }

                _completionComboBox = value;
                if (_completionComboBox != null)
                {
                    _completionComboBox.DropDownOpened += CompletionComboBoxOnDropDownOpened;
                    _completionComboBox.DropDownClosed += CompletionComboBoxOnDropDownClosed;
                    _completionComboBox.PreviewKeyDown += CompletionComboBoxOnPreviewKeyDown;
                    _completionComboBox.GotFocus += CompletionComboBoxOnGotFocus;
                    _completionComboBox.LostFocus += CompletionComboBoxOnLostFocus;
                }
            }
        }

        private void CompletionComboBoxOnLostFocus(object sender, RoutedEventArgs e)
        {
            // _textCaret.BeginAnimation(VisibilityProperty, null);
        }

        private void CompletionComboBoxOnGotFocus(object sender, RoutedEventArgs e)
        {
            DebugFn("Ending caret blink");
            TextCaret.BeginAnimation(VisibilityProperty, null);
        }

        private void CompletionComboBoxOnPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                e.Handled = true;
                InCompletion = false;
                CompletionPopup.IsOpen = false;
            }
        }

        private void CompletionComboBoxOnDropDownClosed(object? sender, EventArgs e)
        {
            CompletionPopup.IsOpen = false;
        }

        private void CompletionComboBoxOnDropDownOpened(object? sender, EventArgs e)
        {
        }

        public Popup CompletionPopup { get; set; }

        protected override void OnPreviewTextInput(TextCompositionEventArgs e)
        {
            base.OnPreviewTextInput(e);
            if (FindPopup.IsOpen)
                return;
            if (_handlingInput)
            {
                var msg = "Dropped input " + e.Text;
                DebugFn(msg);
                ProtoLogger.Instance.LogAction(msg);
                return;
            }

            DebugFn("*** TEXT INPUT ***");
            var eText = e.Text;
            e.Handled = true;
            _handlingInput = true;
            Status = CodeControlStatus.InputHandling;
            JTF.RunAsync(async () =>
            {
                await DoInputAsync(new InputRequest(InputRequestKind.TextInput, eText))
                    .ConfigureAwait(true);
                _handlingInput = false;
                Status = CodeControlStatus.Idle;
            });
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
            c.LineSpacing = f.LineSpacing;
            c.LineHeight = f.LineSpacing * c.FontSize;
            c._typefaceName = f.FamilyNames[XmlLanguage.GetLanguage("en-US")];
        }

        private static void OnFontSizeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var c = ((RoslynCodeControl) d);
            var textCaret = c.TextCaret;

            var newVal = (double) e.NewValue;
            if (textCaret != null) textCaret.LineHeight = newVal * 1.1;
            c.LineHeight = newVal * c.LineSpacing;
            
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
            var length = CustomTextSource.Length - 2;
            var b = InsertionPoint < length;
            var canMoveRightByCharacter = CustomTextSource != null && !_handlingInput && b;
            DebugFn($"Can move right by character {canMoveRightByCharacter}");
            return canMoveRightByCharacter;
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
            // ReSharper disable once RedundantJumpStatement
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

            mr?.Set();
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
            await UpdateRoslynPropertiesAsync();
            if (InCompletion)
            {
                CompletionText += inputRequest.Text;
                CompletionComboBox.Text = CompletionText;
                var completionService = CompletionService.GetService(Document);

                try
                {
                    var results = await completionService.GetCompletionsAsync(Document, InsertionPoint);
                    DumpCompletions(results);

                    CompletionComboBox.ItemsSource = results.Items;
                }
                catch
                {
                    InCompletion = false;
                    CompletionPopup.IsOpen = false;
                }
            }
            else
            {
                if (DoCompletionOnTextInput && inputRequest.Kind == InputRequestKind.TextInput)
                    await DoCompletionAsync(inputRequest.Text[0]);
            }
#endif
            InsertionPoint = complete.NewInsertionPoint;

            return complete;
        }

        public bool DoCompletionOnTextInput { get; } = false;

        public string CompletionText { get; set; }


        public async Task<UpdateComplete> DoUpdateTextAsync(int insertionPoint, InputRequest inputRequest)
        {
            DebugFn($"DoUpdateTextAsync [{insertionPoint}] {inputRequest}");

            var insLine = InsertionLine;
            var insertionLineOffset = insLine?.Offset ?? 0;
            var originY = insLine?.Origin.Y ?? 0;
            var originX = insLine?.Origin.X ?? XOffset;
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
            UpdateComplete updateComplete = default;
            try
            {
                updateComplete = await UpdateCompleteChannel.Reader.ReadAsync();
            }
            catch (TaskCanceledException ex1)
            {
                ProtoLogger.Instance.LogAction(ex1.ToString());
            }
            catch (OperationCanceledException ex2)
            {
                ProtoLogger.Instance.LogAction(ex2.ToString());
            }
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
            TextCaret.BeginAnimation(VisibilityProperty, _x1);
        }

        /// <inheritdoc />
        protected override void OnLostKeyboardFocus(KeyboardFocusChangedEventArgs e)
        {
            base.OnLostKeyboardFocus(e);
            TextCaret.BeginAnimation(VisibilityProperty, null);
        }

        #endregion

        public static RedrawLineResult RedrawLine(RenderRequestInput renderRequestInput,
            FontRendering currentRendering, TextChange? change, LineInfo2? curLineInfo)
        {
            var (roslynCodeControl, lineNo, offset, y, x, textFormatter, paragraphWidth, pixelsPerDip, source,
                _, _, _, _, _) = renderRequestInput;
            var begin = DateTime.Now;
            DebugDelegate debugFn = roslynCodeControl.DebugFn;
#if DEBUG
            debugFn(nameof(RedrawLine));
#endif

            var lineOriginPoint = new Point(x, y);

            double width, height;
            var dg = new DrawingGroup();
            var dc = dg.Open();
            LineInfo2 lineInfo2;
            var runsInfos = new List<TextRunInfo>();
            var rb = CustomTextSource4.RunsBefore(offset, source.Runs);
            var rbi = CustomTextSource4.RunInfosBefore(offset, source.RunInfos);
            var runCount = rb.Count();

            if (runCount == 0)
            {
            }
#if DEBUG
            debugFn("Run count is 0", 2);
#endif


            var allCharInfos = change.HasValue && curLineInfo?.FirstCharInfo != null
                ? curLineInfo?.FirstCharInfo.List
                : new LinkedList<CharInfo>();
            var newLineInfo = false;
            var d = paragraphWidth - lineOriginPoint.X;
            if (d < 0)
                d = 0;
            using (var myTextLine = textFormatter.FormatLine(source,
                offset, d,
                new GenericTextParagraphProperties(currentRendering,
                    pixelsPerDip), null))
            {
#if DEBUG
                debugFn($"got a text line of length {myTextLine.Length}", 4);
#endif
                var textStorePosition = offset;
                // ReSharper disable once PossibleNullReferenceException
                var nRuns = source.Runs.Count - runCount;
                debugFn($"nRuns is {nRuns}");
                var nRunInfos = rbi.Count();


                LinkedListNode<CharInfo> curCharInfoNode=null;
                // ReSharper disable once NotAccessedVariable
                LinkedListNode<CharInfo> lastCharInfoNode;
                CommonText.HandleLine(allCharInfos, lineOriginPoint, myTextLine, source, runCount,
                    nRuns, lineNo, textStorePosition, runsInfos, 
                    curCharInfoNode,out lastCharInfoNode,
                    debugFn, change, curLineInfo);

                source.RunInfos = source.RunInfos.Take(nRunInfos).Concat(runsInfos).ToList();
                myTextLine.Draw(dc, lineOriginPoint, InvertAxes.None);

                width = myTextLine.Width;
                height = myTextLine.Height;

                if (curLineInfo != null)
                {
                    lineInfo2 = curLineInfo;
                    // curLineInfo.Value = new LineInfo2(lineInfo2.LineNumber,
                    // lineInfo2.FirstCharInfo ?? allCharInfos.First, lineInfo2.Offset, myTextLine.Height,
                    // myTextLine.Length);

                    curLineInfo.Height = myTextLine.Height;
                    curLineInfo.Length = myTextLine.Length;
                    curLineInfo.FirstCharInfo ??= allCharInfos.First;
                }
                else
                {
                    lineInfo2 = new LineInfo2(lineNo, allCharInfos.First, textStorePosition,
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

            if (inDg.Bounds.IsEmpty && res.LineInfo.Length > 2)
                throw new InvalidOperationException("Drawing group has empty bounds");
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
            roslynCodeControl1.DrawingBrush2.Viewbox = roslynCodeControl1.DrawingBrushViewbox;
            roslynCodeControl1.Rectangle.Width = width;
            roslynCodeControl1.Rectangle.Height = height;

            LinkedListNode<LineInfo2> llNode = null;
            var setInsertionLineNode = false;
            if (@in.RedrawLineResult.IsNewLineInfo)
            {
                roslynCodeControl1.DrawLineNumber(res.LineInfo.LineNumber, res.LineInfo.Origin);

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
            var prevLineOffset = res.LineInfo.Offset - 2;
            if (newIp == nextLineOffset)
            {
                if (!@in.RedrawLineResult.IsNewLineInfo)
                    llNode = roslynCodeControl1.FindLine(res.LineInfo.LineNumber, roslynCodeControl1.InsertionLineNode);
                // ReSharper disable once PossibleNullReferenceException
                // ReSharper disable once PossibleNullReferenceException
                var lineInfoHeight = Math.Max(res.LineInfo.Height, roslynCodeControl1.LineHeight);
                var origin = new Point(res.LineInfo.Origin.X, res.LineInfo.Origin.Y + lineInfoHeight);

                roslynCodeControl1.DrawLineNumber(res.LineInfo.LineNumber+1, origin);

                llNode = llNode.List.AddAfter(llNode,
                    new LineInfo2(res.LineInfo.LineNumber + 1, null, nextLineOffset,
                        origin, 0, 0));
                setInsertionLineNode = true;
            } else if (newIp == prevLineOffset)
            {
                llNode = roslynCodeControl1.FindLine(res.LineInfo.LineNumber - 1);
                setInsertionLineNode = true;
                // do we need to remove the line ??
            }

            if (setInsertionLineNode)
                roslynCodeControl1.InsertionLineNode = llNode;

            roslynCodeControl1.DebugFn("return");
        }

        public override void DrawLineNumber(int lineNumber, Point lineOrigin)
        {
            string lineNoStr = (lineNumber + 1).ToString("D5");

            var ft = new FormattedText(lineNoStr, CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
                LineNumberTypeface, LineNumberEmSize,
                LineNumberBrush, null, PixelsPerDip);

            var dc1 = LineNumbersDrawingGroup.Append();
            dc1.DrawText(ft, new Point(0, lineOrigin.Y));
            dc1.Close();
        }

        public double LineNumberEmSize => FontSize;

        public Brush LineNumberBrush { get; set; } = Brushes.SlateGray;

        public Typeface LineNumberTypeface { get; set; } = new Typeface(new FontFamily("Lucida Console"),
            FontStyles.Normal, FontWeights.Normal, FontStretches.Normal);


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
            var scrollBar = (ScrollBar) ScrollViewer?.Template.FindName("PART_VerticalScrollBar", ScrollViewer);

            OutputWidth = ScrollViewer?.ActualWidth - scrollBar?.ActualWidth - Rectangle?.StrokeThickness * 2 ?? 0.0;
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
            // return;
            try
            {
                var charInfoNode = InsertionCharInfoNode;
                if (charInfoNode == null)
                {
#if DEBUG
                    _debugFn?.Invoke($"{nameof(UpdateCaretPosition)}  {nameof(InsertionCharInfoNode)} is null.");
#endif
                    if (InsertionLine != null) charInfoNode = InsertionLine?.FirstCharInfo;
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
                        TextCaret.SetValue(Canvas.TopProperty, ciYOrigin);
                        var ciAdvanceWidth = ci.XOrigin + ci.AdvanceWidth - DrawingBrush.Viewbox.Left;
                        TextCaret.SetValue(Canvas.LeftProperty, ciAdvanceWidth);
                        DebugFn($"Caret1 - {ciAdvanceWidth}x{ciYOrigin}");
                        return;
                    }
                    else
                    {
                        // ReSharper disable once PossibleNullReferenceException
                        var ciYOrigin = InsertionLine?.Origin.Y ?? 0 - DrawingBrush.Viewbox.Top;
                        TextCaret.SetValue(Canvas.TopProperty, ciYOrigin);
                        var ciAdvanceWidth = XOffset + -1 * DrawingBrush.Viewbox.Left;
                        TextCaret.SetValue(Canvas.LeftProperty, ciAdvanceWidth);

                        DebugFn($"Caret2 - {ciAdvanceWidth}x{ciYOrigin}");

                        MaxY = Math.Max(MaxY, ciYOrigin - DrawingBrush.Viewbox.Top + TextCaret.LineHeight);
                        Rectangle.Height = MaxY;
                        DrawingBrush.Viewbox = DrawingBrushViewbox = new Rect(DrawingBrushViewbox.X,
                            DrawingBrushViewbox.Y,
                            DrawingBrushViewbox.Width, MaxY);
                        return;
                    }
                }
                else
                {

                    var ci = charInfoNode.Value;
                    var ciYOrigin = ci.YOrigin - DrawingBrush.Viewbox.Top;
                    TextCaret.SetValue(Canvas.TopProperty, ciYOrigin);
                    var ciAdvanceWidth = ci.XOrigin - DrawingBrush.Viewbox.Left;
                    TextCaret.SetValue(Canvas.LeftProperty, ciAdvanceWidth);
                    DebugFn($"Caret1 - {ciAdvanceWidth}x{ciYOrigin}");
                    return;
                }

                if (prevCharInfoNode != null)
                {
                    var ci = prevCharInfoNode.Value;
                    var ciYOrigin = ci.YOrigin - DrawingBrush.Viewbox.Top;
                    TextCaret.SetValue(Canvas.TopProperty, ciYOrigin);
                    var ciAdvanceWidth = ci.XOrigin + ci.AdvanceWidth - DrawingBrush.Viewbox.Left;
                    TextCaret.SetValue(Canvas.LeftProperty,
                        ciAdvanceWidth);

                    DebugFn($"Caret3 - {ciAdvanceWidth}x{ciYOrigin}");
                    return;
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
        // ReSharper disable once NotAccessedField.Local
        private Task _renderChannelReaderTask;
        // ReSharper disable once NotAccessedField.Local
        private Task _postUpdateReaderFaultContinuation;
        private readonly JoinableTaskFactory _myJoinableTaskFactory;
        private readonly JoinableTaskCollection _taskCollection;
        private TypeInfo _typeInfo;
        private AdhocWorkspace _workspace;
        private GeometryGroup _selectionGeometry;
        private int _endOffset;
        private ComboBox _completionComboBox;
        private Popup _findPopup;
        private TextBox _findTextBox;
        private Rect _drawingBrushViewbox;
        private Rect _textSearchInstanceRect;
        private Size ExpandFoundTextRectSize { get; set; }= new Size(3, 3);

        #region Mouse

        /// <inheritdoc />
        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (!_enableMouse) return;
            DrawingContext dc = null;
            JTF.RunAsync(() => MouseMoveAsync(e, dc));
        }

        private async Task MouseMoveAsync(MouseEventArgs e, DrawingContext dc)
        {
            try
            {
                var point = e.GetPosition(Rectangle);
                var b = VisualTreeHelper.GetContentBounds(Rectangle);
                // DebugFn(b.ToString());
                // DebugFn(point.ToString());
                if (!b.Contains(point)) return;
                if (CustomTextSource?.RunInfos == null) return;
                point.Offset(DrawingBrushViewbox.X, DrawingBrushViewbox.Y);
                var runInfo = CustomTextSource.RunInfos.Where(zz1 => zz1.Rect.Contains(point)).ToList();
                if (!runInfo.Any()) SetNoneHover();
#if DEBUG
                // _debugFn?.Invoke(runInfo.Count().ToString());
#endif
                if (!runInfo.Any())
                    return;
                var first = runInfo.First();
#if DEBUG
                // _debugFn?.Invoke(first.Rect.ToString());
#endif
                if (first.TextRun == null) return;
#if DEBUG
                // _debugFn?.Invoke(first.TextRun.ToString() ?? "");
#endif
                if (first.TextRun is CustomTextCharacters c0)
                {
#if DEBUG
                    _debugFn?.Invoke(c0.Text, 5);
#endif
                }

                // fake out hover region info
                var hoverRegionInfo = new RegionInfo(first.TextRun, first.Rect, first.StartCharInfo);
                var ci = hoverRegionInfo.FirstCharInfo;
                CharInfo civ = null;
                var charRect = Rect.Empty;
                while (ci != null)
                {
                    var r = new Rect(ci.Value.XOrigin, ci.Value.YOrigin, ci.Value.AdvanceWidth, first.Rect.Height);

                    if (r.Contains(point))
                    {
                        charRect = r;
                        civ = ci.Value;
                        break;
                    }

                    ci = ci.Next;
                }

                HoverRegionInfo = hoverRegionInfo;
                int? newHoverOffset = null;
                QuickInfoItem? quickInfo = null;
                if (civ != null)
                {
                    newHoverOffset = civ.Index;
                    quickInfo = await QuickInfoService.GetService(Document)
                        .GetQuickInfoAsync(Document, newHoverOffset.Value);
                    if (quickInfo != null)
                    {

                    }
                }

                SyntaxNode newHoverSyntaxNode = null;
                SyntaxToken? newHoverSyntaxToken = null;
                if (first.TextRun is SyntaxTokenTextCharacters stc)
                {
                    newHoverSyntaxNode = stc.Node;
                    newHoverSyntaxToken = stc.Token;
                }

                if (newHoverSyntaxNode != HoverSyntaxNode)
                {
                }

                if (ToolTip is ToolTip tt) tt.IsOpen = false;
                ISymbol sym = null;
                IOperation operation = null;
                var nodes = new Stack<SyntaxNodeDepth>();
                if (newHoverSyntaxNode != null)
                {
                    if (SemanticModel != null)
                        try
                        {
                            sym = SemanticModel?.GetDeclaredSymbol(newHoverSyntaxNode);
                            operation = SemanticModel.GetOperation(newHoverSyntaxNode);
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

                    var node = newHoverSyntaxNode;
                    var depth = 0;
                    while (node != null)
                    {
                        node = node.Parent;
                        depth++;
                    }

                    depth--;
                    node = newHoverSyntaxNode;
                    while (node != null)
                    {
                        nodes.Push(new SyntaxNodeDepth {SyntaxNode = node, Depth = depth});
                        node = node.Parent;
                        depth--;
                    }
                }

                var content = new CodeToolTipContent()
                {
                    Symbol = sym,
                    SyntaxNode = newHoverSyntaxNode,
                    Nodes = nodes,
                    Operation = operation,
                    QuickInfoItem = quickInfo
                };
                var template =
                    TryFindResource(new DataTemplateKey(typeof(CodeToolTipContent))) as DataTemplate;
                var toolTip = new ToolTip {Content = content, ContentTemplate = template};
                ToolTip = toolTip;
                toolTip.IsOpen = true;
            
                
                HoverToken = newHoverSyntaxToken;
                HoverSyntaxNode = newHoverSyntaxNode;
                if (newHoverOffset.HasValue)
                    HoverOffset = newHoverOffset.Value;

                ComputeSelection(newHoverOffset, charRect);

                if (!SelectionEnabled || e.LeftButton != MouseButtonState.Pressed) return;
                if (IsSelecting) return;
                var xy = e.GetPosition(ScrollViewer);
                if (!(xy.X < ScrollViewer.ViewportWidth) || !(xy.X >= 0) || !(xy.Y >= 0) ||
                    !(xy.Y <= ScrollViewer.ViewportHeight)) return;

                _startOffset = HoverOffset;
                _startRow = HoverRow;
                _startColumn = HoverColumn;
                _startNode = HoverSyntaxNode;
                DebugFn("Selecting");
                IsSelecting = true;
                _selectionGeometry = new GeometryGroup();
                e.Handled = true;
                Rectangle.CaptureMouse();
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

        private void ComputeSelection(int? newHoverOffset, Rect charRect)
        {
            if (!SelectionEnabled || !IsSelecting) return;
            if (_endOffset != newHoverOffset.Value)
                return;
#if DEBUG
            _debugFn?.Invoke("Calculating selection");
#endif

            var group = new DrawingGroup();

            // ReSharper disable once NotAccessedVariable
            int begin;
            int end;
            if (_startOffset < newHoverOffset)
            {
                // ReSharper disable once RedundantAssignment
                begin = _startOffset;
                end = newHoverOffset.Value;
            }
            else
            {
                // ReSharper disable once RedundantAssignment
                begin = newHoverOffset.Value;
                end = _startOffset;
            }

            // ReSharper disable once UnusedVariable
            var green = new SolidColorBrush(Colors.Green) {Opacity = .2};
            // ReSharper disable once UnusedVariable
            var blue = new SolidColorBrush(Colors.Blue) {Opacity = .2};
            var red = new SolidColorBrush(Colors.Red) {Opacity = .2};
            _selectionGeometry.Children.Add(new RectangleGeometry(charRect));
            DebugFn($"{_selectionGeometry.Bounds}", 0);
            _endOffset = end;

#if false
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
                                    group.Children.Add(new GeometryDrawing(red, null,
                                        new RectangleGeometry(tuple1.Bounds)));
                                }

                                continue;
                            }

                            if (regionInfo.Offset + regionInfo.Length > end)
                            {
                                foreach (var tuple1 in regionInfo.Characters.Take(end - regionInfo.Offset))
                                    group.Children.Add(new GeometryDrawing(blue, null,
                                        new RectangleGeometry(tuple1.Bounds)));

                                continue;
                            }

                            var geo = new RectangleGeometry(regionInfo.BoundingRect);
                            group.Children.Add(new GeometryDrawing(green, null, geo));
                        }
#endif
            DebugFn($@"{group.Bounds}", 0);


            SelectionDrawing.Children[0] = new GeometryDrawing(null, new Pen(red, 1), _selectionGeometry);
            _selectionEnd = newHoverOffset.Value;
        }

        private void SetNoneHover()
        {
            HoverColumn = 0;
            HoverSyntaxNode = null;
            HoverOffset = 0;
            HoverRegionInfo = null;
            HoverRow = 0;
            HoverSymbol = null;
            HoverToken = null;
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
                                    var r = ModelExtensions.AnalyzeDataFlow(SemanticModel, st1, st2);
                                    if (r != null)
                                        return;
#if DEBUG
                                    _debugFn?.Invoke((r != null && r.Succeeded).ToString());
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

        #endregion

        // ReSharper disable once AutoPropertyCanBeMadeGetOnly.Global
        public bool SelectionEnabled { get; set; } = true;

        public bool IsSelecting { get; set; }

        public double InitialScrollPosition { get; set; }

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
            ScrollViewer?.ScrollToTop();
            // if (SecondaryDispatcher != null)
            // await UpdateTextSourceAsync();

            //UpdateFormattedText();
        }


        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void Shutdown()
        {
            PostUpdateChannel.Writer.Complete();
            RenderChannel.Writer.Complete();
            UpdateCompleteChannel.Writer.Complete();
            UpdateChannel.Writer.Complete();
            foreach (var joinableTask in _taskCollection) DebugFn("Task " + joinableTask);
            JTF.Run(_taskCollection.JoinTillEmptyAsync);
            DebugFn("return from shutdown");
        }

        public async Task ShutdownAsync()
        {
            PostUpdateChannel.Writer.Complete();
            RenderChannel.Writer.Complete();
            UpdateCompleteChannel.Writer.Complete();
            UpdateChannel.Writer.Complete();
            foreach (var joinableTask in _taskCollection) DebugFn("Task " + joinableTask);
            await _taskCollection.JoinTillEmptyAsync();
            DebugFn("return from shutdown");
        }

        #region ISCrollInfo implementation
        /// <inheritdoc />
        public void LineDown()
        {
            
        }

        public LineInfo2? FirstVisibleLine { get; set; }

        /// <inheritdoc />
        public void LineLeft()
        {
        }

        /// <inheritdoc />
        public void LineRight()
        {
        }

        /// <inheritdoc />
        public void LineUp()
        {
        }

        /// <inheritdoc />
        public Rect MakeVisible(Visual visual, Rect rectangle)
        {
            return default;
        }

        /// <inheritdoc />
        public void MouseWheelDown()
        {
        }

        /// <inheritdoc />
        public void MouseWheelLeft()
        {
        }

        /// <inheritdoc />
        public void MouseWheelRight()
        {
        }

        /// <inheritdoc />
        public void MouseWheelUp()
        {
        }

        /// <inheritdoc />
        public void PageDown()
        {
        }

        /// <inheritdoc />
        public void PageLeft()
        {
        }

        /// <inheritdoc />
        public void PageRight()
        {
        }

        /// <inheritdoc />
        public void PageUp()
        {
        }

        /// <inheritdoc />
        public void SetHorizontalOffset(double offset)
        {
        }

        /// <inheritdoc />
        public void SetVerticalOffset(double offset)
        {
        }

        /// <inheritdoc />
        public bool CanHorizontallyScroll
        {
            get { return false; }
            set { }
        }

        /// <inheritdoc />
        public bool CanVerticallyScroll { get; set; }

        /// <inheritdoc />
        public double ExtentHeight
        {
            get { return TextDestination.Bounds.Height; }
        }

        /// <inheritdoc />
        public double ExtentWidth
        {
            get { return TextDestination.Bounds.Width; }
        }

        /// <inheritdoc />
        public double HorizontalOffset { get; }

        /// <inheritdoc />
        public ScrollViewer ScrollOwner
        {
            get { return ScrollViewer; }
            set { }
        }

        /// <inheritdoc />
        public double VerticalOffset { get; }

        /// <inheritdoc />
        public double ViewportHeight { get; }

        /// <inheritdoc />
        public double ViewportWidth { get; }
        #endregion
    }

    public class ContentChangedRoutedEventArgs : RoutedEventArgs
    {
        /// <inheritdoc />
        public ContentChangedRoutedEventArgs(object source) : base(RoslynCodeControl.ContentChangedEvent, source)
        {
        }
    }
}