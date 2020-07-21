using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.Threading;
using RoslynCodeControls;

namespace UnitTestProject1
{
    [TestClass]
    public class UnitTest1
    {
        private RoslynCodeControl _control;
        private Window _window;
        private MyFixture2 _f;
        private bool _closeWindow=true;

        public UnitTest1()
        {
            _f = new MyFixture2();
            
        }

        [STATestMethod]
        public async Task TestMethod1()
        {
            Thread.CurrentThread.Name = "Main1";
            _control = new RoslynCodeControl();
            await _f.InitializeAsync();
            
            var c = _control;
            c.JTF2 = _f.JTF2;
            c.SourceText = "";
       
            var w = new Window();
            _window = w;
            w.Content = _control;

            JoinableTask jt1 = null;
            w.Loaded += (sender, args) =>
            {
                jt1 = c.JTF.RunAsync(DoInput1Async);
                var continueWith = jt1.JoinAsync().ContinueWith(async t =>
                {
                    if (c.IsFaulted)
                    {
                        

                    }
                    await c.Shutdown();

                    c.Dispatcher.InvokeShutdown();
                    c.SecondaryDispatcher.InvokeShutdown();
                    
                }, CancellationToken.None, TaskContinuationOptions.None, TaskScheduler.FromCurrentSynchronizationContext());

            };

            try
            {
                w.Show();
            }
            catch (Exception ex)
            {
                _f.Debugfn(ex.ToString());
            }
            Dispatcher.Run();
            await jt1.JoinAsync();
        }

        private async Task DoInput1Async()
        {
            EventSource e = new EventSource("Test1");
            var cTempProgram0Cs = @"C:\temp\program.cs";
            var csFile = Environment.GetEnvironmentVariable("CSFILE");
            if (csFile != null)
            {
                cTempProgram0Cs = csFile;
            }
            var code = File.ReadAllLines(cTempProgram0Cs);
            var c2 = _control;
            Debug.WriteLine("loaded");
            List<TimeSpan> timings = new List<TimeSpan>();
            List<double> avgs = new List<double>();
            
            await c2.UpdateFormattedTextAsync();
            // var success2 = await c2.DoInputAsync(new InputRequest(InputRequestKind.NewLine));
            var now0 = DateTime.Now;
            int ip = 0;
            foreach (var s in code)
            {
                foreach (var ch in s)
                {
                    // if (now0 + new TimeSpan(0, 1, 0) < DateTime.Now)
                    // {
                    // _window.Close();
                    // return;
                    // }
                    _f.Debugfn("" + ch + " insertion point is " + c2.InsertionPoint);
                    var now1 = DateTime.Now;

                    var success = await c2.DoInputAsync(new InputRequest(InputRequestKind.TextInput, ch.ToString()));
                    ip++;
                    var successRenderRequestTimestamp = success.RenderRequestTimestamp - now1;
                    var postUpdateTimestamp = success.PostUpdateTimestamp - now1;
                    var completedTimestamp = success.Timestamp - now1;
                    var successRenderBeganTimestamp = success.RenderBeganTimestamp - now1;
                    var successRenderCompleteTimestamp = success.RenderCompleteTimestamp - now1;
                    _f.Debugfn(successRenderRequestTimestamp.ToString());
                    _f.Debugfn(postUpdateTimestamp.ToString());
                    _f.Debugfn(completedTimestamp.ToString());
                    var elapsed = DateTime.Now - now1;

                    timings.Add(elapsed);
                    _f.Debugfn("took " + elapsed);

                    if (timings.Count % 20 == 1)
                    {
                        var avg = timings.Average(span => span.TotalMilliseconds);
                        var averageIs = "Average is " + avg;

                        
                        avgs.Add(avg);
                    }


                }

                await c2.DoInputAsync(new InputRequest(InputRequestKind.NewLine));
            }

            if (_window != null)
            {
                var dpi = VisualTreeHelper.GetDpi(_window);
                var bmp = new RenderTargetBitmap((int)_window.ActualWidth, (int)_window.ActualHeight, dpi.PixelsPerInchX, dpi.PixelsPerInchY, System.Windows.Media.PixelFormats.Pbgra32);
                bmp.Render(_window);
                var zz = new PngBitmapEncoder();
                zz.Frames.Add(BitmapFrame.Create(bmp));
                using (FileStream stream = new FileStream(@"c:\temp\window.png", FileMode.Create))
                {
                    zz.Save(stream);
                }
            }

            if (_closeWindow)
            {
                _window?.Close();

            }
        }
    }
}
