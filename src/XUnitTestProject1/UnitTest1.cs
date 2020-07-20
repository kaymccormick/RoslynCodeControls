using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using System.Windows.Xps.Packaging;
using Microsoft.VisualStudio.Threading;
using RoslynCodeControls;
using Xunit;

namespace XUnitTestProject1
{
    public class UnitTest1 : IClassFixture<MyFixture>
    {
        private readonly MyFixture _f;

        public UnitTest1(MyFixture f)
        {
            Debug.WriteLine(f);
            _f = f;
        }

        [WpfFact]
        public void Test5()
        {
            RoslynCodeControl c= new RoslynCodeControl();
            c.JTF2 = _f.JTF2;
            c.JTF = new JoinableTaskFactory(new JoinableTaskContext());
            c.SourceText = "";
            c.UpdateFormattedTextAsync();
            Window w = new Window();
            w.Content = c;
            w.Loaded += (sender, args) => Debug.WriteLine("loaded");
            RenderTargetBitmap b = new RenderTargetBitmap(1024, 1024, 96, 96, PixelFormats.Pbgra32);
            b.Render(c);
            PngBitmapEncoder pngImage = new PngBitmapEncoder();
            pngImage.Frames.Add(BitmapFrame.Create(b));
            using (Stream fileStream = File.Create(@"C:\temp\1.png"))
            {
                pngImage.Save(fileStream);
            }
            w.ShowDialog();
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

        public JoinableTaskFactory JTF2 { get; set; }

        /// <inheritdoc />
        public Task DisposeAsync()
        {
            return Task.CompletedTask;
        }
    }
}