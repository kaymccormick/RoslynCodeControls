using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Printing;
using System.Runtime.CompilerServices;
using System.Text;
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
using System.Windows.Xps.Packaging;
using JetBrains.Annotations;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.Threading;
using RoslynCodeControls;
using Path = System.IO.Path;

namespace NewTestapp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private JoinableTaskFactory _f;
        private RoslynPaginator _dp;
        private Info1 _info;

        public MainWindow()
        {
            _context = new JoinableTaskContext();
            _coll = _context.CreateCollection();
            _f = _context.CreateFactory(_coll);
            InitializeComponent();
            _host = MefHostServices.Create(MefHostServices.DefaultAssemblies);

            // _f = new JoinableTaskFactory(new JoinableTaskContext());
            // cb.JTF = _f;
           Loaded += OnLoaded;
           
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            _f.RunAsync(M1);
        }


        public JoinableTaskFactory JTF2 { get; set; }

        public static readonly DependencyProperty CurPageProperty = DependencyProperty.Register(
            "CurPage", typeof(int), typeof(MainWindow), new PropertyMetadata(-1, OnCurPageChanged));

        private XpsDocument _xps2;
        private MefHostServices _host;
        private VisualBrush pageBrush;
        private double _pageScale1=1.0;

        public int CurPage
        {
            get { return (int) GetValue(CurPageProperty); }
            set { SetValue(CurPageProperty, value); }
        }

        public Info1 Info
        {
            get { return _info; }
            set
            {
                if (Equals(value, _info)) return;
                _info = value;
                OnPropertyChanged();
            }
        }

        private static void OnCurPageChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((MainWindow) d).OnCurPageChanged((int) e.OldValue, (int) e.NewValue);
        }



        protected virtual void OnCurPageChanged(int oldValue, int newValue)
        {
            if (newValue >= 0)
            {
                NewMethod(newValue);
            }

            
        }

        private void NewMethod(int newVlue)
        {
            if (_dp != null)
            {
                // DrawingGroup dg = new DrawingGroup();
                // var dc = dg.Open();
                // _dp.DrawPage(newVlue, dc, out _);
                // dc.Close();
                // var w = new DGWindow(dg);
                // w.Show();

                // Debug.WriteLine(dg.Bounds);

                var p = _dp.GetDocumentPage(newVlue, out var info);
                scroll.ScrollToTop();
                scroll.ScrollToLeftEnd();
                Info = info;
                pageBrush = new VisualBrush(p.Visual);
                pageBrush.AlignmentX=AlignmentX.Left;
                pageBrush.AlignmentY = AlignmentY.Top;
                var dpi1 = VisualTreeHelper.GetDpi(p.Visual);
                var dpi2 = VisualTreeHelper.GetDpi(this);
                pageBrush.Stretch = Stretch.None;
                var b1 = VisualTreeHelper.GetContentBounds(p.Visual);
                rect.Width = b1.Width;
                rect.Height = b1.Height;
                // var zz = VisualTreeHelper.GetContentBounds(p.Visual);
                // b.Viewbox = zz;
                // b.ViewboxUnits = BrushMappingMode.Absolute;
                // b.Viewport = new Rect(0, 0, rect.ActualWidth, rect.ActualHeight);
                // b.ViewportUnits = BrushMappingMode.Absolute;
            
                Debug.WriteLine(rect.ActualWidth);
                Debug.WriteLine(rect.ActualHeight);
                rect.Fill = pageBrush;
            }
        }


        private async Task M1()
        {
            cb = new RoslynCodeBase(DebugFn)
            {
                JTF2 = JTF2,
                SourceText = Filename != null ? File.ReadAllText(Filename) : "",
                DocumentTitle = Filename ?? "Untitled",
                Rectangle = new Rectangle(),
                FontSize = 14.0,
                FontFamily = new FontFamily("Lucida Console")
            };
            ;
            await cb.UpdateFormattedTextAsync();
            
            DirectoryInfo d = new DirectoryInfo(@"C:\temp\code");

            LocalPrintServer s = new LocalPrintServer();
            var pdfQueues = s.GetPrintQueues().Where(queue => queue.FullName.ToLowerInvariant().Contains("pdf") && !queue.IsInError && !queue.IsOffline).ToList();
            PrintDialog pd1  = new PrintDialog();
            //pd1.ShowDialog();
            _dp = (RoslynPaginator)new RoslynPaginator(cb);
            PageCount = _dp.PageCount;
            // pd1.PrintDocument(_dp, "code");
            
            // PrintQueue q;
            // if (pdfQueues.Count() == 1)
            // {
                // q = pdfQueues.First();
            // }
            // else
            // {
                // q = pdfQueues.Skip(1).First();
            // }
            
            var tf = Path.Combine(d.FullName, Path.ChangeExtension(Path.GetFileName(Path.GetTempFileName()), ".xps"));
                //Path.GetTempFileName();

            var _xpsDocument = new XpsDocument(tf,
                FileAccess.ReadWrite);

            var xpsDocumentWriter= XpsDocument.CreateXpsDocumentWriter(_xpsDocument);

            xpsDocumentWriter.Write(_dp);
            _xpsDocument.Close();
            var f2 = Path.Combine(d.FullName, Path.ChangeExtension(Path.GetFileName(Path.GetTempFileName()), ".xps"));
            File.Copy(tf, f2);
            var f3 = Path.Combine(d.FullName, Path.ChangeExtension(Path.GetFileName(Path.GetTempFileName()), ".xps"));
            File.Copy(tf, f3);

            // var j = q.AddJob("code", f2, false);
            //j.Commit();
            _xps2 = new XpsDocument(f2, FileAccess.Read);
            DocView.Document = _xps2.GetFixedDocumentSequence();
            // grid.Children.Remove(cb);
            // DocView.Document = cb;
            var b1 = _dp._bmp;
            ImageBrush b2 = new ImageBrush(b1);
            rect2.Fill = b2;
            AllPagesBitmap = b1;
            b2.Stretch = Stretch.Uniform;
            CurPage = 0;
            
        }

        private static void DebugFn(string obj, int debugLevel=10)
        {
            Debug.WriteLine(obj);
        }

        public static readonly DependencyProperty AllPagesBitmapProperty = DependencyProperty.Register(
            "AllPagesBitmap", typeof(ImageSource), typeof(MainWindow), new PropertyMetadata(default(ImageSource), OnAllPagesBitmapChanged));

        public ImageSource AllPagesBitmap
        {
            get { return (ImageSource) GetValue(AllPagesBitmapProperty); }
            set { SetValue(AllPagesBitmapProperty, value); }
        }

        private static void OnAllPagesBitmapChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((MainWindow) d).OnAllPagesBitmapChanged((ImageSource) e.OldValue, (ImageSource) e.NewValue);
        }



        protected virtual void OnAllPagesBitmapChanged(ImageSource oldValue, ImageSource newValue)
        {
        }

        public int PageCount { get; set; }

        public static readonly DependencyProperty ViewportXProperty = DependencyProperty.Register(
            "ViewportX", typeof(double), typeof(MainWindow), new PropertyMetadata(default(double), OnViewportXChanged));

        public double ViewportX
        {
            get { return (double) GetValue(ViewportXProperty); }
            set { SetValue(ViewportXProperty, value); }
        }

        private static void OnViewportXChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((MainWindow) d).OnViewportXChanged((double) e.OldValue, (double) e.NewValue);
        }



        protected virtual void OnViewportXChanged(double oldValue, double newValue)
        {
            Viewport = new Rect(newValue, Viewport.Y, Viewport.Width, Viewport.Height);
        }

        public static readonly DependencyProperty ViewportYProperty = DependencyProperty.Register(
            "ViewportY", typeof(double), typeof(MainWindow), new PropertyMetadata(default(double), OnViewportYChanged));

        public double ViewportY
        {
            get { return (double) GetValue(ViewportYProperty); }
            set { SetValue(ViewportYProperty, value); }
        }

        private static void OnViewportYChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((MainWindow) d).OnViewportYChanged((double) e.OldValue, (double) e.NewValue);
        }



        protected virtual void OnViewportYChanged(double oldValue, double newValue)
        {
            Viewport = new Rect(Viewport.X, newValue, Viewport.Width, Viewport.Height);
        }


        public static readonly DependencyProperty ViewportWidthProperty = DependencyProperty.Register(
            "ViewportWidth", typeof(double), typeof(MainWindow), new PropertyMetadata(default(double), OnViewportWidthChanged));

        public double ViewportWidth
        {
            get { return (double) GetValue(ViewportWidthProperty); }
            set { SetValue(ViewportWidthProperty, value); }
        }

        private static void OnViewportWidthChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((MainWindow) d).OnViewportWidthChanged((double) e.OldValue, (double) e.NewValue);
        }



        protected virtual void OnViewportWidthChanged(double oldValue, double newValue)
        {
            Viewport = new Rect(Viewport.X, Viewport.Y, newValue, Viewport.Height);
        }

        public static readonly DependencyProperty ViewportHeightProperty = DependencyProperty.Register(
            "ViewportHeight", typeof(double), typeof(MainWindow), new PropertyMetadata(default(double), OnViewportHeightChanged));

        public double ViewportHeight
        {
            get { return (double) GetValue(ViewportHeightProperty); }
            set { SetValue(ViewportHeightProperty, value); }
        }

        private static void OnViewportHeightChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((MainWindow) d).OnViewportHeightChanged((double) e.OldValue, (double) e.NewValue);
        }



        protected virtual void OnViewportHeightChanged(double oldValue, double newValue)
        {
                Viewport = new Rect(Viewport.X, Viewport.Y, Viewport.Width, newValue);
        }

        public static readonly DependencyProperty ViewboxXProperty = DependencyProperty.Register(
            "ViewboxX", typeof(double), typeof(MainWindow), new FrameworkPropertyMetadata(default(double), OnViewboxXChanged));

        public double ViewboxX
        {
            get { return (double) GetValue(ViewboxXProperty); }
            set { SetValue(ViewboxXProperty, value); }
        }

        private static void OnViewboxXChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((MainWindow) d).OnViewboxXChanged((double) e.OldValue, (double) e.NewValue);
        }


        protected virtual void OnViewboxXChanged(double oldValue, double newValue)
        {
            Viewbox = new Rect(newValue, Viewbox.Y, Viewbox.Width, Viewbox.Height);
        }

        public static readonly DependencyProperty ViewboxYProperty = DependencyProperty.Register(
            "ViewboxY", typeof(double), typeof(MainWindow), new PropertyMetadata(default(double), OnViewboxYChanged));

        public double ViewboxY
        {
            get { return (double) GetValue(ViewboxYProperty); }
            set { SetValue(ViewboxYProperty, value); }
        }

        private static void OnViewboxYChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((MainWindow) d).OnViewboxYChanged((double) e.OldValue, (double) e.NewValue);
        }



        protected virtual void OnViewboxYChanged(double oldValue, double newValue)
        {
            Viewbox = new Rect(Viewbox.X, newValue, Viewbox.Width, Viewbox.Height);
        }

        public static readonly DependencyProperty ViewboxWidthProperty = DependencyProperty.Register(
            "ViewboxWidth", typeof(double), typeof(MainWindow), new PropertyMetadata(default(double), OnViewboxWidthChanged));

        public double ViewboxWidth
        {
            get { return (double) GetValue(ViewboxWidthProperty); }
            set { SetValue(ViewboxWidthProperty, value); }
        }

        private static void OnViewboxWidthChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((MainWindow) d).OnViewboxWidthChanged((double) e.OldValue, (double) e.NewValue);
        }
        


        protected virtual void OnViewboxWidthChanged(double oldValue, double newValue)
        {
            Viewbox = new Rect(Viewbox.X, Viewbox.Y, newValue, Viewbox.Height);
        }

        public static readonly DependencyProperty ViewboxHeightProperty = DependencyProperty.Register(
            "ViewboxHeight", typeof(double), typeof(MainWindow), new PropertyMetadata(default(double), OnViewboxHeightChanged));
        
        public double ViewboxHeight
        {
            get { return (double) GetValue(ViewboxHeightProperty); }
            set { SetValue(ViewboxHeightProperty, value); }
        }

        private static void OnViewboxHeightChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((MainWindow) d).OnViewboxHeightChanged((double) e.OldValue, (double) e.NewValue);
        }



        protected virtual void OnViewboxHeightChanged(double oldValue, double newValue)
        {
            Viewbox = new Rect(Viewbox.X, Viewbox.Y, Viewbox.Width, newValue);
            
        }

        public static readonly DependencyProperty ViewboxProperty = DependencyProperty.Register(
            "Viewbox", typeof(Rect), typeof(MainWindow), new FrameworkPropertyMetadata(default(Rect), FrameworkPropertyMetadataOptions.AffectsRender,OnViewboxChanged));

        public Rect Viewbox
        {
            get { return (Rect) GetValue(ViewboxProperty); }
            set { SetValue(ViewboxProperty, value); }
        }

        private static void OnViewboxChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((MainWindow) d).OnViewboxChanged((Rect) e.OldValue, (Rect) e.NewValue);
        }



        protected virtual void OnViewboxChanged(Rect oldValue, Rect newValue)
        {
            Debug.WriteLine("Viewbox now " + newValue);
            Debug.WriteLine(ImageBrush.Viewbox);
        }


        public static readonly DependencyProperty ViewportProperty = DependencyProperty.Register(
            "Viewport", typeof(Rect), typeof(MainWindow), new PropertyMetadata(default(Rect), OnViewportChanged));

        private JoinableTaskContext _context;
        private JoinableTaskCollection _coll;
        private RoslynCodeBase cb;

        public Rect Viewport
        {
            get { return (Rect) GetValue(ViewportProperty); }
            set { SetValue(ViewportProperty, value); }
        }

        public string Filename { get; set; }

        private static void OnViewportChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((MainWindow) d).OnViewportChanged((Rect) e.OldValue, (Rect) e.NewValue);
        }



        protected virtual void OnViewportChanged(Rect oldValue, Rect newValue)
        {
            Debug.WriteLine(newValue);
        }

        private void ButtonBase_OnClick(object sender, RoutedEventArgs e)
        {
            NewMethod(CurPage);
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void ButtonBase_OnClick1(object sender, RoutedEventArgs e)
        {

        }

        private void ZoomPage(object sender, RoutedEventArgs e)
        {
            //pageBrush.Viewbox = new Rect(0, 0, rect.ActualWidth, rect.ActualHeight);
            _pageScale1 /= 0.33;
            pageBrush.RelativeTransform = new ScaleTransform(_pageScale1, _pageScale1, 0.0, 0.0);
        }

        private void PrevPage(object sender, RoutedEventArgs e)
        {
            if(CurPage > 0)
            {
                CurPage--;
            }
        }

        private void NextPage(object sender, RoutedEventArgs e)
        {
            if (CurPage < PageCount - 1)
            {
                CurPage++;
            }
        }
    }

    internal class DGWindow : Window
    {
        public DGWindow(DrawingGroup dg)
        {
            DrawingVisual v = new DrawingVisual();
            var dc = v.RenderOpen();
            dc.DrawDrawing(dg);
            dc.Close();
            Content = new DGElement(dg);
        }

    }

    internal class DGElement : UIElement
    {
        private Drawing dg;

        public DGElement(Drawing dg)
        {
            this.dg = dg;
        }

        /// <inheritdoc />
        protected override void OnRender(DrawingContext drawingContext)
        {
            drawingContext.DrawDrawing(dg);
        }
    }
}
