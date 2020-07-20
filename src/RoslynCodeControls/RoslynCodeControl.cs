using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
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
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Threading;
using TextLine = System.Windows.Media.TextFormatting.TextLine;

// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
#pragma warning disable 162

// ReSharper disable ConvertToUsingDeclaration

namespace RoslynCodeControls
{
    /// <summary>
    /// 
    /// </summary>
    public class RoslynCodeControl : RoslynCodeBase, ILineDrawer, INotifyPropertyChanged, IFace1, ICodeView
    {
        public RoslynCodeControl()
        {
            _textDestination = new DrawingGroup();
            MyDrawingBrush = new DrawingBrush()
            {
                AlignmentX = AlignmentX.Left,
                AlignmentY = AlignmentY.Top,
                TileMode = TileMode.None,
                ViewboxUnits = BrushMappingMode.Absolute,
                Stretch = Stretch.None,
                Drawing = _textDestination
            };
            BindingOperations.SetBinding(MyDrawingBrush, TileBrush.ViewboxProperty,
                new Binding("DrawingBrushViewbox") { Source = this });
            // _documentPaginator = new RoslynPaginator(this);

            Rectangle = new Rectangle();
            Rectangle.SetBinding(WidthProperty,
                new Binding("DrawingBrushViewbox.Width") { Source = this });
            Rectangle.SetBinding(HeightProperty,
                new Binding("DrawingBrushViewbox.Height") { Source = this });

            
            CustomTextSourceProxy = new CustomTextSource4Proxy(this);
            UpdateChannel = Channel.CreateUnbounded<UpdateInfo>(new UnboundedChannelOptions()
            { SingleReader = true, SingleWriter = true });
            _reader = UpdateChannel.Reader;
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

            // CSharpCompilationOptions = new CSharpCompilationOptions(default(OutputKind));
            PixelsPerDip = VisualTreeHelper.GetDpi(this).PixelsPerDip;
            // CommandBindings.Add(new CommandBinding(WpfAppCommands.Compile, CompileExecuted));
            SetupCommands(this, this);
        }

        public static readonly DependencyProperty FilenameProperty = DependencyProperty.Register(
            "Filename", typeof(string), typeof(SyntaxNodeControl), new PropertyMetadata(default(string), OnFilenameChanged));
        public static readonly DependencyProperty InsertionPointProperty = DependencyProperty.Register(
            "InsertionPoint", typeof(int), typeof(RoslynCodeControl),
            new PropertyMetadata(default(int), OnInsertionPointChanged, CoerceInsertionPoint));
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
            get { return (string)GetValue(FilenameProperty); }
            set { SetValue(FilenameProperty, value); }
        }

        private static void OnFilenameChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((RoslynCodeControl)d).OnFilenameChanged((string)e.OldValue, (string)e.NewValue);
        }


        public static readonly RoutedEvent RenderCompleteEvent = EventManager.RegisterRoutedEvent("RenderComplete",
            RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(RoslynCodeControl));

        public static readonly RoutedEvent RenderStartEvent = EventManager.RegisterRoutedEvent("RenderStart",
            RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(RoslynCodeControl));

     
        public LinkedListNode<CharInfo> InsertionCharInfoNode
        {
            get { return _insertionCharInfoNode; }
            set
            {
                if (Equals(value, _insertionCharInfoNode)) return;
                _insertionCharInfoNode = value;
                OnPropertyChanged();
            }
        }

        public override CharInfo InsertionCharInfo
        {
            get { return (CharInfo) GetValue(InsertionCharInfoProperty); }
            set { SetValue(InsertionCharInfoProperty, value); }
        }

      
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

        public override int InsertionPoint
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

        protected virtual void OnInsertionPointChanged(int oldValue, int newValue)
        {
            if (newValue == -1)
            {
            }

            if (!_updatingCaret)
                UpdateCaretPosition(oldValue, newValue);
            return;
            try
            {
                var enclosingSymbol = SemanticModel?.GetEnclosingSymbol(newValue);
                EnclosingSymbol = enclosingSymbol;

                if (EnclosingSymbol != null)
                    Debug.WriteLine(EnclosingSymbol.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat));
                if (SemanticModel != null && InsertionRegion.SyntaxNode != null)
                {
                    var ti = SemanticModel.GetTypeInfo(InsertionRegion.SyntaxNode);
                    if (ti.Type != null)
                        Debug.WriteLine(ti.Type.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat));
                }
            }
            catch (Exception)
            {
                // ignored
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

        /// <summary>
        /// 
        /// </summary>
        protected bool UiLoaded { get; set; }

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


        protected virtual void OnTextSourceTextChanged(string oldValue, string newValue)
        {
        }
        #region Private members
        private Rect _rect;
        private DrawingBrush _myDrawingBrush;
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

        private int _selectionEnd;
        private SyntaxNode _startNode;
        private SyntaxNode _endNode;
        private SourceText _text;
        private ObjectAnimationUsingKeyFrames _x1;
        private ISymbol _enclosingSymbol;
        private DispatcherOperation<Task> _updateOperation;
        private bool _performingUpdate;
        private DispatcherOperation<CustomTextSource4> _innerUpdateDispatcherOperation;

        private ChannelReader<UpdateInfo> _reader;
        private bool _handlingInput;
        private bool _updatingCaret;
        private Rectangle _rectangle;
        private LinkedListNode<CharInfo> _insertionCharInfoNode;
        private LinkedListNode<LineInfo2> _insertionLineNode;
        private Rect _drawingBrushViewbox;
        private DrawingGroup _textDestination;
        #endregion

        #region Public properties
        public Rectangle Rectangle
        {
            get { return _rectangle; }
            set
            {
                _rectangle = value;
                if (_rectangle != null)
                {
                }
            }
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

        public DrawingBrush MyDrawingBrush
        {
            get { return _myDrawingBrush; }
            set
            {
                if (Equals(value, _myDrawingBrush)) return;
                _myDrawingBrush = value;
                OnPropertyChanged();
            }
        }


        public LineInfo2 FirstLine
        {
            get { return LineInfos2?.First?.Value; }
        }

        public LinkedList<CharInfo> CharInfos { get; set; } = new LinkedList<CharInfo>();

        public override DrawingBrush DrawingBrush => _myDrawingBrush;

        public override DrawingGroup TextDestination => _textDestination;
        public override Rect DrawingBrushViewbox
        {
            get { return (Rect)GetValue(DrawingBrushViewboxProperty); }
            set { SetValue(DrawingBrushViewboxProperty, value); }
        }
        #endregion

        static RoslynCodeControl()
        {
            FontSizeProperty.OverrideMetadata(typeof(RoslynCodeControl),
                new FrameworkPropertyMetadata(OnFontSizeChanged));
            FocusableProperty.OverrideMetadata(typeof(RoslynCodeControl), new FrameworkPropertyMetadata(true));
            DefaultStyleKeyProperty.OverrideMetadata(typeof(RoslynCodeControl),
                new FrameworkPropertyMetadata(typeof(RoslynCodeControl)));
            SyntaxTreeProperty.OverrideMetadata(typeof(RoslynCodeControl),
                new FrameworkPropertyMetadata(default(SyntaxTree), FrameworkPropertyMetadataOptions.None,
                    OnSyntaxTreeChanged_));
            SyntaxNodeProperty.OverrideMetadata(typeof(RoslynCodeControl),
                new PropertyMetadata(default(SyntaxNode), OnNodeUpdated));
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

        protected virtual void OnDrawingBrushViewboxChanged(Rect oldValue, Rect newValue)
        {
        }

        
        #region Input hadling
        protected override void OnPreviewKeyDown(KeyEventArgs e)
        {
            base.OnPreviewKeyDown(e);
            if (e.Handled) return;
            switch (e.Key)
            {
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
            if (!b)
                Debug.WriteLine("Backspace failed");
            _handlingInput = false;
            Status = CodeControlStatus.Idle;
        }
        #endregion
        public UIElement Debug2Container { get; set; }

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

       
        #region Channel
        private void ContinuationFunction(Task<UpdateInfo> z)
        {
            
            var ui = z.Result;
            Debug.WriteLine("ContinuationFunction ");
            // fixme
            //CharInfos.AddRange(ui.CharInfos);
            var dg = ui.DrawingGroup;
            var dg2 = new DrawingGroup();
            foreach (var dgChild in dg.Children)
                //dg.Children.Remove(dgChild);
                dg2.Children.Add(dgChild);
            TextDestination.Children.Add(dg2);
            var uiRect = dg2.Bounds;
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
            _reader.ReadAsync().AsTask().ContinueWith(ContinuationFunction, CancellationToken.None,
                TaskContinuationOptions.None, TaskScheduler.FromCurrentSynchronizationContext());
        }

#endregion

        #region Text drawing
        /// <inheritdoc />
        public Task<CustomTextSource4> InnerUpdate(MainUpdateParameters mainUpdateParameters, TextSourceInitializationParameters textSourceInitializationParameters)
        {
            return CommonText.InnerUpdate(mainUpdateParameters, () =>
            {
                
                return CreateCustomTextSource4(textSourceInitializationParameters);
            });
        }
#endregion

        private async void OnEnterLineBreak(object sender, ExecutedRoutedEventArgs e)
        {
            if (_handlingInput)
                return;
            _handlingInput = true;
            Status = CodeControlStatus.InputHandling;
            var b = await DoInputAsync(new InputRequest(InputRequestKind.NewLine)).ConfigureAwait(true);
            if (!b)
                Debug.WriteLine("Newline failed");
            _handlingInput = false;
            Status = CodeControlStatus.Idle;
        }

        private void CanEnterLineBreak(object sender, CanExecuteRoutedEventArgs e)
        {
            if (_handlingInput) return;
            e.CanExecute = true;
            e.Handled = true;
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
            // if (!UiLoaded)
            // return;
            if (Compilation != null && Compilation.SyntaxTrees.Contains(SyntaxTree) == false)
                throw new InvalidOperationException();

            if (SyntaxNode == null || SyntaxTree == null) return;
            if (ReferenceEquals(SyntaxNode.SyntaxTree, SyntaxTree) == false)
                throw new InvalidOperationException("SyntaxNode is not within syntax tree");

            await CommonText.UpdateFormattedText(this);
        }


        

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
            Debug1Container = (UIElement) GetTemplateChild("debug1container");
            Debug2Container = (UIElement) GetTemplateChild("debug2container");
            Debug3Container = (UIElement) GetTemplateChild("debug3container");
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
            Debug.WriteLine(OutputWidth.ToString());

            var templateChild = (Rectangle) GetTemplateChild("Rectangle");
            if (templateChild != null)
            {
                Rectangle = templateChild;

                Rectangle.SetBinding(WidthProperty,
                    new Binding("DrawingBrushViewbox.Width") {Source = this});
                Rectangle.SetBinding(HeightProperty,
                    new Binding("DrawingBrushViewbox.Height") {Source = this});
                Rectangle.Fill = MyDrawingBrush;
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
            UiLoaded = true;
        }

        public UIElement Debug3Container { get; set; }

        public UIElement Debug1Container { get; set; }
        #region thread
        public static Thread StartSecondaryThread(ManualResetEvent mevent, Action<object> cb)
        {
            var t = new ParameterizedThreadStart(SecondaryThreadStart);
            var newWindowThread = SecondaryThread = new Thread(t);
            newWindowThread.SetApartmentState(ApartmentState.STA);
            newWindowThread.Name = "SecondaryThread";
            newWindowThread.IsBackground = true;
            newWindowThread.Start(mevent);
            return newWindowThread;
        }

        public static Thread SecondaryThread { get; set; }
        private static void SecondaryThreadStart(object o)
        {
            ManualResetEvent mr = (ManualResetEvent) o;
            
            var d = Dispatcher.CurrentDispatcher;
            // Dispatcher.Invoke(() =>
            // {
            StaticSecondaryDispatcher = d;
            // });
            mr.Set();
            Dispatcher.Run();
        }

        public static Dispatcher StaticSecondaryDispatcher { get; set; }

        public Dispatcher SecondaryDispatcher
        {
            get { return StaticSecondaryDispatcher; }
        }
        #endregion
        public TranslateTransform Translate { get; set; }


        /// <summary>
        /// 
        /// </summary>
        public LineInfo2 InsertionLine
        {
            get { return InsertionLineNode?.Value; }
        }

        public LinkedListNode<LineInfo2> InsertionLineNode
        {
            get { return _insertionLineNode; }
            set
            {
                if (Equals(value, _insertionLineNode)) return;
                _insertionLineNode = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(InsertionLine));
            }
        }


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
                Status = CodeControlStatus.InputHandling;
                    await DoInputAsync(new InputRequest(InputRequestKind.TextInput, eText)).ConfigureAwait(true);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
                throw;
            }
            finally
            {
                _handlingInput = false;
                Status = CodeControlStatus.Idle;
            }
        }

        public async Task<bool> DoInputAsync(InputRequest inputRequest)
        {
            var text = inputRequest.Text;
            try
            {
                if (CustomTextSource == null)
                {
                    //await UpdateTextSource().ConfigureAwait(true);

                    TextSourceInitializationParameters arg;
                    TextSourceInitializationParameters textSourceInitializationParameters;
                    textSourceInitializationParameters = CreateDefaultTextSourceArguments();
                    await JTF2.SwitchToMainThreadAsync();
                    CustomTextSource = CreateCustomTextSource4(textSourceInitializationParameters);
                    await JTF.SwitchToMainThreadAsync();
                }

                Debug.WriteLine(text);

                var insertionPoint = InsertionPoint;
                if (inputRequest.Kind != InputRequestKind.Backspace)
                {
                }
                else
                {
                }

                await DoUpdateTextAsync(insertionPoint, inputRequest).ConfigureAwait(true);

                ChangingText = true;
                SyntaxNode = CustomTextSource.Node;
                SyntaxTree = CustomTextSource.Tree;
                ChangingText = false;

                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
                throw;
            }
        }

        private async Task DoUpdateTextAsync(int insertionPoint, InputRequest inputRequest)
        {
            try
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


                var inn = new CallbackParameters1(this, insertionLineLineNumber, insertionLineOffset, originY, originX,
                    insertionLine, Formatter, OutputWidth, null, PixelsPerDip, CustomTextSource, MaxY, MaxX,
                    d, drawingContext, FontSize, typefaceName, FontWeight);

                await JTF2.SwitchToMainThreadAsync();
                var lineInfo = await Callback(inn, insertionPoint, inputRequest);
                var in2 = new CallbackParameters2(this, insertionPoint, inputRequest, text, inn, lineInfo);
                await JTF.SwitchToMainThreadAsync();
                await Callback2(in2);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                throw;
            }
        }

        private static async Task Callback2(CallbackParameters2 callbackParameters2)
        {
            var roslynCodeControl = callbackParameters2.RoslynCodeControl;
            var inputRequest = callbackParameters2.InputRequest;
            var lineInfo = callbackParameters2.LineInfo;
            var insertionPoint = callbackParameters2.InsertionPoint;
            var text = callbackParameters2.Text;
            var inn = callbackParameters2.In1;
            if (inputRequest.Kind == InputRequestKind.Backspace)
                roslynCodeControl.InsertionPoint--;
            else
                roslynCodeControl.InsertionPoint = insertionPoint + (text?.Length ?? 0);

            if (lineInfo == null) throw new InvalidOperationException();

            if (roslynCodeControl.InsertionPoint == lineInfo.Offset + lineInfo.Length)
            {
                var newLineInfo = new LineInfo2(lineInfo.LineNumber + 1, null,
                    roslynCodeControl.InsertionPoint, new Point(
                        roslynCodeControl.XOffset,
                        roslynCodeControl.InsertionLine.Origin.Y + roslynCodeControl.InsertionLine.Height),
                    lineInfo.Height, lineInfo.Length);

                var drawingGroup = new DrawingGroup();
                var dc = drawingGroup.Open();
                var inn2 = new CallbackParameters1(roslynCodeControl, newLineInfo.LineNumber, newLineInfo.Offset,
                    newLineInfo.Origin.Y, newLineInfo.Origin.X, newLineInfo, Formatter, inn.ParagraphWidth,
                    inn.CurrentRendering, inn.PixelsPerDip, inn.CustomTextSource4, inn.MaxY, inn.MaxX, drawingGroup, dc,
                    inn.FontSize, inn.FontFamilyName, inn.FontWeight);
                await roslynCodeControl.JTF2.SwitchToMainThreadAsync();

                await Callback3(inn2);
                
             
                roslynCodeControl.UpdateCaretPosition();
            }

         
        }

        private static async Task<LineInfo2> Callback(CallbackParameters1 inn, int insertionPoint, InputRequest inputRequest)
        {
            var text = inputRequest.Text;
            try
            {
                inn.CurrentRendering = FontRendering.CreateInstance(inn.FontSize, TextAlignment.Left,
                    new TextDecorationCollection(), Brushes.Black,
                    new Typeface(new FontFamily(inn.FontFamilyName), FontStyles.Normal, inn.FontWeight,
                        FontStretches.Normal));
                inn.CustomTextSource4?.TextInput(insertionPoint, inputRequest);

                var redrawLine = await RedrawLine(inn);
                return redrawLine;
            }
            catch (Exception ex)

            {
                Debug.WriteLine(ex.ToString());
            }

            return null;
        }

        private static async Task<LineInfo2> Callback3(CallbackParameters1 inn)
        {
            try
            {
                inn.CurrentRendering = FontRendering.CreateInstance(inn.FontSize, TextAlignment.Left,
                    new TextDecorationCollection(), Brushes.Black,
                    new Typeface(new FontFamily(inn.FontFamilyName), FontStyles.Normal, inn.FontWeight,
                        FontStretches.Normal));
                var lineInfo = await RedrawLine(inn);
                return lineInfo;
            }
            catch (Exception ex)

            {
                Debug.WriteLine(ex.ToString());
            }

            return null;
        }


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
        private static async Task<LineInfo2> RedrawLine(CallbackParameters1 callbackParameters1)
        {
            Debug.WriteLine("redrawline");
            LineInfo outLineInfo;
            LineInfo prevLine = null;
            var lineNo = callbackParameters1.LineNo;
            var lineOriginPoint = new Point(callbackParameters1.X, callbackParameters1.Y);
            LinkedListNode<LineInfo2> llNode = null;
            var roslynCodeControl = callbackParameters1.RoslynCodeControl;
            var origin = new Point(lineOriginPoint.X, lineOriginPoint.Y);
            double width, height;
            using (var myTextLine = callbackParameters1.TextFormatter.FormatLine(callbackParameters1.CustomTextSource4,
                callbackParameters1.Offset, callbackParameters1.ParagraphWidth,
                new GenericTextParagraphProperties(callbackParameters1.CurrentRendering, callbackParameters1.PixelsPerDip), null))
            {
                var runCount = callbackParameters1.CustomTextSource4.RunInfos?.Count(ri => true) ?? 0;
                var textStorePosition = callbackParameters1.Offset;
                var allCharInfos = new LinkedList<CharInfo>();
                // _ = CommonText.HandleTextLine(ref textStorePosition, out _, ref prevLine, ref lineNo, lineOriginPoint,
                // callbackParameters1.ParagraphWidth, callbackParameters1.CustomTextSource4, runCount, myTextLine, allCharInfos,
                // callbackParameters1.CustomTextSource4.RunInfos, callbackParameters1.Dc, out outLineInfo, false,
                // roslynCodeControl.Dispatcher);

                await roslynCodeControl.JTF.SwitchToMainThreadAsync();
                myTextLine.Draw(callbackParameters1.Dc, origin, InvertAxes.None);
                await roslynCodeControl.JTF2.SwitchToMainThreadAsync();
                LinkedListNode<LineInfo2> li0 = null;
                width = myTextLine.Width;
                height = myTextLine.Height;
                
                var lineInfo2 = new LineInfo2(callbackParameters1.LineNo, allCharInfos.First, textStorePosition,
                    origin, myTextLine.Height, myTextLine.Length);
                li0 = roslynCodeControl.FindLine(callbackParameters1.LineNo);
                if (li0 == null)
                {
                    li0 = roslynCodeControl.FindLine(callbackParameters1.LineNo - 1);
                    if (li0 != null)
                    {
                        llNode = roslynCodeControl.LineInfos2.AddAfter(li0, lineInfo2);
                    }
                    else
                    {
                        if (roslynCodeControl.LineInfos2.Any()) throw new InvalidOperationException();
                        llNode = roslynCodeControl.LineInfos2.AddFirst(lineInfo2);
                        roslynCodeControl.OnPropertyChanged(nameof(FirstLine));
                    }
                }
                else
                {
                    if (Equals(roslynCodeControl.LineInfos2.First, li0))
                        roslynCodeControl.OnPropertyChanged(nameof(FirstLine));
                    li0.Value = lineInfo2;
                    roslynCodeControl.OnPropertyChanged(nameof(InsertionLine));
                    llNode = li0;
                }

                roslynCodeControl.InsertionLineNode = llNode;
            }
#if false
                lineCtx = new LineContext()
                {
                    LineNumber = callbackParameters1.LineNo,
                    CurCellRow = callbackParameters1.LineNo,
                    // LineInfo = callbackParameters1.LineInfo,
                    LineOriginPoint = lineOriginPoint,
                    MyTextLine = myTextLine,
                    MaxX = callbackParameters1.MaxX,
                    MaxY = callbackParameters1.MaxY,
                    TextStorePosition = callbackParameters1.Offset
                };

                var o = lineCtx.LineOriginPoint;
                callbackParameters1.Dc.Dispatcher.Invoke(() => { myTextLine.Draw(callbackParameters1.Dc, o, InvertAxes.None); });
                var regions = new List<RegionInfo>();
                FormattingHelper.HandleTextLine(regions, ref lineCtx, out var lineI, callbackParameters1.RoslynCodeControl);
#endif
            var lineCtxMaxX = origin.X + width;
            var lineCtxMaxY = origin.Y + height;
            // for(int i = 0; i < 10; i++)
            // CustomTextSource4.DoEvents();
            void PostUpdate()
            {
                callbackParameters1.Dc.Close();
                var textDest = roslynCodeControl.TextDestination;
                var i = callbackParameters1.LineNo / 100;
                var j = callbackParameters1.LineNo % 100;
                if (textDest.Children.Count <= i)
                {
                    var drawingGroup = new DrawingGroup();
                    for (var k = 0; k < j; k++) drawingGroup.Children.Add(new DrawingGroup());
                    drawingGroup.Children.Add(callbackParameters1.D);
                    textDest.Children.Add(drawingGroup);
                }
                else
                {
                    var drawingGroup = (DrawingGroup) textDest.Children[i];
                    for (var k = 0; k < j; k++) drawingGroup.Children.Add(new DrawingGroup());

                    if (j >= drawingGroup.Children.Count)
                        drawingGroup.Children.Add(callbackParameters1.D);
                    else
                        drawingGroup.Children[j] = callbackParameters1.D;
                }


                var maxX = Math.Max(roslynCodeControl.MaxX, lineCtxMaxX);
                roslynCodeControl.MaxX = maxX;
                var maxY = Math.Max(roslynCodeControl.MaxY, lineCtxMaxY);
                roslynCodeControl.MaxY = maxY;
                // bound to viewbox height / width
                // roslynCodeControl.Rectangle.Width = lineCtxMaxX;
                // roslynCodeControl.Rectangle.Height = lineCtxMaxY;
                roslynCodeControl._rect2.Width = lineCtxMaxX;
                roslynCodeControl._rect2.Height = lineCtxMaxY;

                var boundsLeft = Math.Min(roslynCodeControl.TextDestination.Bounds.Left, 0);
                boundsLeft -= 3;
                var boundsTop = Math.Min(roslynCodeControl.TextDestination.Bounds.Top, 0);
                boundsTop -= 3;
                roslynCodeControl.DrawingBrushViewbox = new Rect(boundsLeft, boundsTop, maxX - boundsLeft, maxY - boundsTop);
            }

            var j1 = new JoinableTaskFactory(new JoinableTaskContext());
            await roslynCodeControl.JTF.SwitchToMainThreadAsync();
            PostUpdate();
            await j1.SwitchToMainThreadAsync();

            return llNode.Value;
        }

        public LinkedListNode<LineInfo2> FindLine(int lineNo)
        {
            LinkedListNode<LineInfo2> li0;
            for (li0 = LineInfos2.First; li0 != null; li0 = li0.Next)
                if (li0.Value.LineNumber == lineNo)
                    break;

            return li0;
        }

        public LinkedList<LineInfo2> LineInfos2 { get; set; } = new LinkedList<LineInfo2>();

      
        /// <summary>
        /// 
        /// </summary>
        // ReSharper disable once MemberCanBePrivate.Global
        public static TextFormatter Formatter { get; } = TextFormatter.Create();

        /// <inheritdoc />
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
                    Debug.WriteLine("already performing update");
                    return arrangeOverride;
                }

                InitialUpdate = false;
                if (SyntaxNode == null)
                    return arrangeBounds;
                Debug.WriteLine("Performing initial update of text");
                var updateFormattedText = CommonText.UpdateFormattedText(this);
                UpdateFormattedTestTask = updateFormattedText;
            }

            return arrangeOverride;
        }
#endif

        public override bool InitialUpdate { get; set; } = true;


        /// <inheritdoc />
        public override DocumentPaginator DocumentPaginator => _documentPaginator;


        public override bool PerformingUpdate
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

        public CustomTextSource4Proxy CustomTextSourceProxy { get; set; }

        

        protected virtual void OnFilenameChanged(string oldValue, string newValue)
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



        private static void OldHandleTextLine(Point linePosition, TextLine myTextLine, LineInfo lineInfo,
            List<char> lineChars,
            Point cell, double cellColumn, CharacterCell prevCell, int characterOffset, Point location,
            IList<TextSpan<TextRun>> spans,
            int regionOffset)
        {
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
            var indexedGlyphRuns = myTextLine.GetIndexedGlyphRuns();
            if (indexedGlyphRuns != null)
                foreach (var rect in indexedGlyphRuns)
                {
                    var rectGlyphRun = rect.GlyphRun;

                    var @group = 0;
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

                        List<char> chars = null;
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


                            // ReSharper disable once PossibleNullReferenceException
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
                            //                                TextDestination.Children.Add(new GeometryDrawing(null, new Pen(Brushes.DarkOrange, 2), new RectangleGeometry(bounds)));
                        }

                        //var bb = rect.GlyphRun.BuildGeometry().Bounds;

                        size.Height += myTextLine.Height;
                        var r = new Rect(location, size);
                        location.Offset(size.Width, 0);
//                            dc.DrawRectangle(null, new Pen(Brushes.Green, 1), r);
                        //rects.Add(r);
                        RegionInfo prevRegion = null;
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
                                node = stc.Node;
                                token = stc.Token;
                            }
                            else
                            {
                                if (textSpanValue is SyntaxTriviaTextCharacters stc2)
                                {
                                    trivia = stc2.Trivia;
                                    AttachedToken = stc2.Token;
                                    attachedNode = stc2.Node;
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
        }

        private void UpdateCaretPosition(int? oldValue = null, int? newValue = null)
        {
            var charInfoNode = InsertionCharInfoNode;
            if (charInfoNode == null)
            {
                Debug.WriteLine($"{nameof(UpdateCaretPosition)}  {nameof(InsertionCharInfoNode)} is null.");
                charInfoNode = FindLine(InsertionLine.LineNumber).Value.FirstCharInfo;
            }

            var f = charInfoNode == null ? true : charInfoNode.Value.Index < newValue;
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
                    var ciYOrigin = ci.YOrigin - MyDrawingBrush.Viewbox.Top;
                    _textCaret.SetValue(Canvas.TopProperty, ciYOrigin);
                    var ciAdvanceWidth = ci.XOrigin + ci.AdvanceWidth - MyDrawingBrush.Viewbox.Left;
                    _textCaret.SetValue(Canvas.LeftProperty, ciAdvanceWidth);
                }
            }
            else
            {
                if (prevCharInfoNode != null)
                {
                    var ci = prevCharInfoNode.Value;
                    _textCaret.SetValue(Canvas.TopProperty, ci.YOrigin - MyDrawingBrush.Viewbox.Top);
                    _textCaret.SetValue(Canvas.LeftProperty,
                        ci.XOrigin + ci.AdvanceWidth - MyDrawingBrush.Viewbox.Left);
                }
            }
#if false
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

            if (ciIndex == -1) ciIndex = 0;

            ci = CharInfos[ciIndex];

            Debug.WriteLine($"Character is {ci.Character}");
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
                    Debug.WriteLine($"New caret position is ( {leftValue} , {ciYOrigin} )");
                    _textCaret.SetValue(Canvas.LeftProperty, leftValue);
                    InsertionCharInfo = ci;
                }
            }
#endif
        }


        /// <summary>
        /// 
        /// </summary>
        public ObservableCollection<LineInfo> LineInfos { get; } = new ObservableCollection<LineInfo>();

        // ReSharper disable once NotAccessedField.Local
        private Border _border;

        // ReSharper disable once NotAccessedField.Local
        private Grid _grid;

        /// <summary>
        /// 
        /// </summary>
        public Typeface Typeface { get; protected set; }

      
        public double XOffset { get; set; } = 0.0;
       

        public Channel<UpdateInfo> UpdateChannel { get; set; }
      
        public RoslynPaginator _documentPaginator { get; set; }

        #region Mouse
        /// <inheritdoc />
        protected override void OnMouseMove(MouseEventArgs e)
        {
            DrawingContext dc = null;
            try
            {
                var point = e.GetPosition(Rectangle);
                if (CustomTextSource?.RunInfos != null)
                {
                    var runInfo = CustomTextSource.RunInfos.Where(zz1 => zz1.Rect.Contains(point)).ToList();
                    if (runInfo.Any())
                    {
                        Debug.WriteLine(runInfo.Count().ToString());
                        var first = runInfo.First();
                        Debug.WriteLine(first.Rect.ToString());
                        if (first.TextRun == null) return;
                        Debug.WriteLine(first.TextRun.ToString() ?? "");
                        if (first.TextRun is CustomTextCharacters c0) Debug.WriteLine(c0.Text);
                        // fake out hover region info
                        HoverRegionInfo = new RegionInfo(first.TextRun, first.Rect, new List<CharacterCell>());
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
                            if (_selectionGeometry != null) TextDestination.Children.Remove(_selectionGeometry);
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
                                if (SemanticModel != null)
                                {
                                    var r = SemanticModel.AnalyzeDataFlow(st1, st2);
                                    if (r != null)
                                        return;
                                    Debug.WriteLine(r != null && r.Succeeded);
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
        public bool SelectionEnabled { get; set; }
        public bool IsSelecting { get; set; }



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
    }
}