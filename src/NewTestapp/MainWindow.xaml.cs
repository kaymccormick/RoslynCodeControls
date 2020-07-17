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
            InitializeComponent();
            var fileName = @"C:\temp\program.cs";
            cb.SourceText = File.ReadAllText(fileName);
            cb.DocumentTitle = fileName;
            _f = new JoinableTaskFactory(new JoinableTaskContext());
            cb.JTF = _f;
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
                var p = _dp.GetDocumentPage(newVlue, out var info);
                Info = info;
                VisualBrush b = new VisualBrush(p.Visual);
                var dpi1 = VisualTreeHelper.GetDpi(p.Visual);
                var dpi2 = VisualTreeHelper.GetDpi(this);
                //b.Stretch = Stretch.Uniform;
                // var zz = VisualTreeHelper.GetContentBounds(p.Visual);
                // b.Viewbox = zz;
                // b.ViewboxUnits = BrushMappingMode.Absolute;
                // b.Viewport = new Rect(0, 0, rect.ActualWidth, rect.ActualHeight);
                // b.ViewportUnits = BrushMappingMode.Absolute;
            
                Debug.WriteLine(rect.ActualWidth);
                Debug.WriteLine(rect.ActualHeight);
                rect.Fill = b;
            }
        }


        private async Task M1()
        {
            cb.JTF2 = JTF2;
            await cb.UpdateFormattedTextAsync();
            DirectoryInfo d = new DirectoryInfo(@"C:\temp\code");

            PrintDialog d1 = new PrintDialog();
            LocalPrintServer s = new LocalPrintServer();
            var pdfQueues = s.GetPrintQueues().Where(queue => queue.FullName.ToLowerInvariant().Contains("pdf") && !queue.IsInError && !queue.IsOffline).ToList();
            PrintDialog pd1  = new PrintDialog();
            pd1.ShowDialog();
            _dp = (RoslynPaginator)new RoslynPaginator(cb);
            pd1.PrintDocument(_dp, "code");
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
            CurPage = 0;

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
    }
}
