using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Windows.Xps.Packaging;
using Microsoft.VisualStudio.Threading;
using RoslynCodeControls;
using Xunit;
using Xunit.Abstractions;

namespace XUnitTestProject1
{
    public class UnitTest1 : IClassFixture<MyFixture>
    {
        private readonly MyFixture _f;
        private readonly ITestOutputHelper _outputHelper;
        private RoslynCodeControl _control;
        private Window _window;
        private RoslynCodeBase _control0;
        private bool _closeWindow=true;

        public UnitTest1(MyFixture f, ITestOutputHelper outputHelper)
        {
            Debug.WriteLine(f);
            _f = f;
            _f.OutputHelper = outputHelper;
            _outputHelper = outputHelper;
        }

        [WpfFact]
        public void Test5()
        {
            // Action<string> debugOut = (s) => _f.Debugfn()
            // {
                // _outputHelper.WriteLine(s);
                // Debug.WriteLine(s);
            // };
            RoslynCodeControl c = new RoslynCodeControl(_f.Debugfn);
            _control = c;
            c.JTF2 = _f.JTF2;
            c.JTF = new JoinableTaskFactory(new JoinableTaskContext());
            c.SourceText = "";
            
            Window w = new Window();
            _window = w;
            w.Content = c;
            w.Loaded += OnWOnLoaded;

            w.ShowDialog();
        }
        [WpfFact]
        public void Test6()
        {
            Action<string> debugOut = (s) => _outputHelper.WriteLine(s);
            RoslynCodeControl c = new RoslynCodeControl(debugOut);
            _control0 = _control = c;
            c.JTF2 = _f.JTF2;
            c.JTF = new JoinableTaskFactory(new JoinableTaskContext());
            c.SourceText = File.ReadAllText(@"C:\temp\dockingmanager.cs");

            Window w = new Window();
            _window = w;
            w.Content = c;
            DateTime StartTime=DateTime.MinValue;
            c.AddHandler(RoslynCodeBase.RenderStartEvent, new RoutedEventHandler((sender, args) =>
            {
                StartTime = DateTime.Now;
                Debug.WriteLine("render start");
            }));
            c.AddHandler(RoslynCodeBase.RenderCompleteEvent, new RoutedEventHandler((sender, args) =>
            {
                var span = DateTime.Now - StartTime;
                var msg1 = "render complete " + span;
                Debug.WriteLine(msg1);
                _outputHelper.WriteLine(msg1);
            }));
            w.Loaded += OnWOnLoaded2;
            _closeWindow = true;
            w.ShowDialog();
        }
        [WpfFact]
        public void Test7()
        {
            Action<string> debugOut = (s) => _outputHelper.WriteLine(s);
            var c = new RoslynCodeBase(debugOut);
            _control0 = c;
            c.JTF2 = _f.JTF2;
            c.JTF = new JoinableTaskFactory(new JoinableTaskContext());
            c.SourceText = File.ReadAllText(@"C:\temp\dockingmanager.cs");

            DateTime StartTime = DateTime.MinValue;
            c.AddHandler(RoslynCodeBase.RenderStartEvent, new RoutedEventHandler((sender, args) =>
            {
                StartTime = DateTime.Now;
                Debug.WriteLine("render start");
            }));
            c.AddHandler(RoslynCodeBase.RenderCompleteEvent, new RoutedEventHandler((sender, args) =>
            {
                var span = DateTime.Now - StartTime;
                Debug.WriteLine("render complete " + span);
            }));
            c.JTF.Run(c.UpdateFormattedTextAsync);
            WriteDocument(c);

        }
        private async void OnWOnLoaded(object sender, RoutedEventArgs args)
        {
            var code = File.ReadAllLines(@"C:\temp\program.cs");
            var c2 = _control;
            Debug.WriteLine("loaded");
            await c2.UpdateFormattedTextAsync();
            // var success2 = await c2.DoInputAsync(new InputRequest(InputRequestKind.NewLine));
            foreach (var s in code)
            {
                foreach (var ch in s)
                {

                    Debug.WriteLine("" + ch + " insertion point is " +c2.InsertionPoint);
            var success = await c2.DoInputAsync(new InputRequest(InputRequestKind.TextInput, ch.ToString()));
            // Debug.WriteLine("Success is " + success);

            // Debug.WriteLine("viewbox " + c2.DrawingBrushViewbox);
            var l = c2.InsertionLine;
            var ci = l.FirstCharInfo;
            var i = 0;
            while (ci != null)
            {

                        _f.Debugfn($"Char[{i}] {ci.Value}");
                        ci = ci.Next;
                        i++;
            }
            // break;

                }

                break;
                await c2.DoInputAsync(new InputRequest(InputRequestKind.NewLine));
            }
            // var bmptmp = BitmapSource.Create(1, 1, 96, 96, PixelFormats.Bgr24, null, new byte[3] { 0, 0, 0 }, 3);

            // double width = 100;
            // double height = 100;
            // var imgcreated = new TransformedBitmap(bmptmp, new ScaleTransform(width, height));
            // Rectangle r = new Rectangle();
            // r.Fill = c2.MyDrawingBrush;
            // r.Width = 100;
            // r.Height = 100;

            // RenderTargetBitmap b = new RenderTargetBitmap(1024, 1024, 96, 96, PixelFormats.Pbgra32);
            // b.Render(r);

            // b.Render(c2);
            // PngBitmapEncoder pngImage = new PngBitmapEncoder();
            // pngImage.Frames.Add(BitmapFrame.Create(b));
            // using (Stream fileStream = File.Create(@"C:\temp\1.png"))
            // {
            // pngImage.Save(fileStream);
            // }
            
            if(_closeWindow)
            _window.Close();

        }
        private async void OnWOnLoaded2(object sender, RoutedEventArgs args)
        {
            var c2 = _control0;
             await c2.UpdateFormattedTextAsync();

             WriteDocument(c2);
             if(_closeWindow)
                 _window.Close();

        }

        // [WpfFact]
        public void Test4()
        {
            var mevent = new ManualResetEvent(false);
            var startSecondaryThread = RoslynCodeControl.StartSecondaryThread(mevent, (d) => { });
            var joinableTaskFactory = new JoinableTaskFactory(new JoinableTaskContext());
            joinableTaskFactory.Run(() => { return X2(mevent, startSecondaryThread, joinableTaskFactory); });
        }

        private async Task X2(ManualResetEvent mevent, Thread startSecondaryThread,
            JoinableTaskFactory joinableTaskFactory)
        {
            await mevent.ToTask();
            await NewMethod2(startSecondaryThread, joinableTaskFactory);
        }

        private static async Task NewMethod2(Thread startSecondaryThread, JoinableTaskFactory jtf)
        {
            var d = Dispatcher.FromThread(startSecondaryThread);

            var jtf2 = new JoinableTaskFactory(new JoinableTaskContext(RoslynCodeControl.SecondaryThread,
                new DispatcherSynchronizationContext(d)));
            var b = await NewMethod(jtf, jtf2);
            await jtf.SwitchToMainThreadAsync();
            WriteDocument(b);
        }

        private static void WriteDocument(RoslynCodeBase b)
        {
            var bDocumentPaginator = b.DocumentPaginator;
            if (bDocumentPaginator == null)
            {
                throw new InvalidOperationException("No paginator");
            }
            // var dg = new PrintDialog();
            // dg.PrintDocument(bDocumentPaginator, "");
            var cTempDoc12Xps = @"c:\temp\doc12.xps";
            File.Delete(cTempDoc12Xps);
            var _xpsDocument = new XpsDocument(cTempDoc12Xps,
                FileAccess.ReadWrite);

            var xpsdw = XpsDocument.CreateXpsDocumentWriter(_xpsDocument);
            if (bDocumentPaginator != null) xpsdw.Write(bDocumentPaginator);
            _xpsDocument.Close();
        }

        // [WpfFact]
        public void Test3()
        {
            var mevent = new ManualResetEvent(false);
            var startSecondaryThread = RoslynCodeControl.StartSecondaryThread(mevent, (d) => { });

            var joinableTaskFactory = new JoinableTaskFactory(new JoinableTaskContext());
            joinableTaskFactory.Run(() => { return X1(mevent, startSecondaryThread, joinableTaskFactory); });
        }

        private async Task X1(ManualResetEvent mevent, Thread startSecondaryThread,
            JoinableTaskFactory joinableTaskFactory)
        {
            await mevent.ToTask();
            await NewMethod1(startSecondaryThread, joinableTaskFactory);
        }

        private static async Task NewMethod1(Thread startSecondaryThread, JoinableTaskFactory jtf)
        {
            var d = Dispatcher.FromThread(startSecondaryThread);

            var jtf2 = new JoinableTaskFactory(new JoinableTaskContext(RoslynCodeControl.SecondaryThread,
                new DispatcherSynchronizationContext(d)));
            var b = await NewMethod(jtf, jtf2);
            await jtf.SwitchToMainThreadAsync();
            var bDocumentPaginator = b.DocumentPaginator;
            // var dg = new PrintDialog();
            // dg.PrintDocument(bDocumentPaginator, "");
            var cTempDoc12Xps = @"c:\temp\doc12.xps";
            File.Delete(cTempDoc12Xps);
            var _xpsDocument = new XpsDocument(cTempDoc12Xps,
                FileAccess.ReadWrite);

            var xpsdw = XpsDocument.CreateXpsDocumentWriter(_xpsDocument);
            xpsdw.Write(bDocumentPaginator);
            _xpsDocument.Close();
        }


        // [WpfFact]
        public void Test2()
        {
            var mevent = new ManualResetEvent(false);
            var startSecondaryThread = RoslynCodeControl.StartSecondaryThread(mevent, (d) => { });

            mevent.ToTask().ContinueWith((B) =>
            {
                var d = Dispatcher.FromThread(startSecondaryThread);

                var jtf2 = new JoinableTaskFactory(new JoinableTaskContext(RoslynCodeControl.SecondaryThread,
                    new DispatcherSynchronizationContext(d)));
                var jtf = new JoinableTaskFactory(new JoinableTaskContext());
                jtf.Run(() => NewMethod(jtf, jtf2));
            }, CancellationToken.None, TaskContinuationOptions.None, TaskScheduler.Current).Wait();
        }

        private static async Task<RoslynCodeBase> NewMethod(JoinableTaskFactory j, JoinableTaskFactory jtf2)
        {
            await j.SwitchToMainThreadAsync();
            var b = new RoslynCodeBase();

            b.JTF = j;
            b.JTF2 = jtf2;
            ICodeView c = b;
            var code = File.ReadAllText(@"C:\temp\program.cs");
            c.SourceText = code;
            var updateFormattedTextAsync = c.UpdateFormattedTextAsync();
            await updateFormattedTextAsync;
            return b;
        }
    }

    public class MyFixture : IAsyncLifetime
    {
        private ITestOutputHelper _outputHelper;

        /// <inheritdoc />
        public async Task InitializeAsync()
        {
            var mevent = new ManualResetEvent(false);
            var startSecondaryThread = RoslynCodeControl.StartSecondaryThread(mevent, (d) => { });
            // var joinableTaskFactory = new JoinableTaskFactory(new JoinableTaskContext());
            
            await mevent.ToTask();
            var d = Dispatcher.FromThread(startSecondaryThread);
            var jtf2 = new JoinableTaskFactory(new JoinableTaskContext(RoslynCodeControl.SecondaryThread,
                new DispatcherSynchronizationContext(d)));
            this.JTF2 = jtf2;

        }

        public void Debugfn(string msg)
        {
            var newmsg = Thread.CurrentThread.ManagedThreadId + ": " + Task.CurrentId + ": " + msg;
            Debug.WriteLine(newmsg);
            _outputHelper.WriteLine(newmsg);
        }

        public JoinableTaskFactory JTF2 { get; set; }

        public ITestOutputHelper OutputHelper
        {
            get { return _outputHelper; }
            set { _outputHelper = value; }
        }

        /// <inheritdoc />
        public Task DisposeAsync()
        {
            return Task.CompletedTask;
        }
    }
}