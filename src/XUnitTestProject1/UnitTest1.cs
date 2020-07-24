using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.IO;
using System.Linq;
using System.Security.Permissions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using System.Windows.Xps.Packaging;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Threading;
using RoslynCodeControls;
using Xunit;
using Xunit.Abstractions;

namespace XUnitTestProject1
{
    internal class MyCompanyEventSource : EventSource
    {
        public static readonly MyCompanyEventSource Log = new MyCompanyEventSource();

        public void Startup()
        {
            WriteEvent(1);
        }

        public void Timing(double timing)
        {
            WriteEvent(2, timing);
        }
    }

    public class UnitTest1 : IAsyncLifetime
    {
        private int debugLevel = 2;
        private MyFixture _f;
        private readonly ITestOutputHelper _outputHelper;
        
        private Window _window;
        
        private bool _closeWindow = true;
        private List<List<TimeSpan>> _spans;
        private RoslynCodeControl _codeControl;

        public UnitTest1(ITestOutputHelper outputHelper)
        {
            _outputHelper = outputHelper;
        }

        public async Task InitializeAsync()
        {
            _f = new MyFixture(debugLevel) {OutputHelper = _outputHelper};
            await _f.InitializeAsync();
            _codeControl = new RoslynCodeControl(_f.Debugfn) {JTF2 = _f.JTF2};
            JTF = _codeControl.JTF;
        }

        public JoinableTaskFactory JTF { get; set; }

        public RoslynCodeControl CodeControl
        {
            get { return _codeControl; }
            set { _codeControl = value; }
        }

        /// <inheritdoc />
        public async Task DisposeAsync()
        {
            await _f.DisposeAsync();
            _f = null;
        }

        [WpfFact]
        public void TestDoInputAsync1()
        {
            NewMethod("a");
        }

        [WpfFact]
        public void TestDoInputAsync2()
        {
            NewMethod("ab");
        }

        private void NewMethod(string input)
        {
            var insertionPoint = 0;
            
            
            Func<RoslynCodeControl,string,TestContext, Task> a = async (rcc,  inputChar,context) =>
            {
                var ir = new InputRequest(InputRequestKind.TextInput, inputChar);
                var done = await rcc.DoUpdateTextAsync(insertionPoint, ir);
                Assert.Equal(inputChar, done.InputRequest.Text);

                Assert.Single(rcc.LineInfos2);
                var il = rcc.InsertionLine;
                Assert.Equal(il, rcc.LineInfos2.First.Value);
                Assert.Equal(0, il.Offset);
                Assert.Equal(context.Length + 3, il.Length);
                Assert.Equal(0, il.LineNumber);
                Assert.Equal(new Point(0, 0), il.Origin);
                var ci = il.FirstCharInfo;


                _f.Debugfn(done.ToString());
            };

            var jt = JTF.RunAsync(async () =>
            {
                await CodeControl.UpdateFormattedTextAsync();
                var context = new TestContext();
                foreach (var ch in input)
                {
                    await a(CodeControl, ch.ToString(), context);
                    context.Length++;
                }
            });

            var continueWith = jt.JoinAsync().ContinueWith(ContinuationFunction);
            continueWith.ContinueWith(t =>
            {
                CodeControl.Shutdown();
                return t.Result;
            });

            //, CancellationToken.None, TaskContinuationOptions.None, TaskScheduler.FromCurrentSynchronizationContext());

            while (!continueWith.IsCompleted)
            {
                DoEvents();
                Thread.Sleep(500);
                _f.Debugfn("loop");
            }


#pragma warning disable VSTHRD002 // Avoid problematic synchronous waits
            Assert.True(continueWith.Result);
#pragma warning restore VSTHRD002 // Avoid problematic synchronous waits

            var s = CodeControl.CustomTextSource;
            Assert.Collection(s.Runs, run => { Assert.IsType<SyntaxTokenTextCharacters>(run); },
                run => { Assert.IsType<CustomTextEndOfParagraph>(run); });
            Assert.Collection(s.RunInfos, runInfo => { Assert.IsType<SyntaxTokenTextCharacters>(runInfo.TextRun); });
        }

        private bool ContinuationFunction(Task t)
        {
            if (t.IsFaulted)
            {
                _f.Debugfn("Task faulted");
                return false;
            }

            if (_codeControl.IsFaulted) _f.OutputHelper.WriteLine("faulted");
            return true;
        }

        private void Run(Func<RoslynCodeControl, Task> func)
        {
            JTF.Run(() => func(CodeControl));
        }

        

        [WpfTheory]
        [InlineData("", 0, 0, 0, 0, 0, 1, 0, 0, 12, "Arial", null,2, true, 0,null)]
        [InlineData("test", 0, 0, 0, 0, 0, 1, 0, 0, 12, "Arial", null,6, true, 1, new[] { 0, 1, 2, 3 })]
        [InlineData("foo\r\nbar", 0, 0, 0, 0, 0, 1, 0, 0, 12, "Arial", null,5, true, 1,  new[]{0, 1, 2})]
        [InlineData("public class A { }\r\n", 0, 0, 0, 0, 0, 1, 0, 0, 12, "Arial", null,20, true, 9, new[] { 0, 1, 2, 3, 4, 5, 0, 0, 1, 2, 3, 4, 0, 0, 0, 0, 0, 0  })]
        [InlineData("public class A { }\r\n", 0, 0, 0, 0, 0, 1, 0, 0, 12, "Arial",new object[]{0,null,0,0.0,0.0,0.0,0,0}, 20, true, 9, new[] { 0, 1, 2, 3, 4, 5, 0, 0, 1, 2, 3, 4, 0, 0, 0, 0, 0, 0 })]
        public void TestRedrawLine1(string code, int lineNo, int offset, double x, double y, double width, double pixelsPerDip, double maxX, double maxY, double fontSize, string fontFamilyName,object[] curLineInfoObjects, int outLength, bool isNewLineInfo, int dgCount, int[] runIndiciesAry)
        {
            var face = new Typeface(new FontFamily(fontFamilyName), FontStyles.Normal, FontWeights.Normal,
                FontStretches.Normal);
            
            var rendering = 
                FontRendering.CreateInstance(fontSize,TextAlignment.Left,new TextDecorationCollection(),Brushes.Black, face);
            
            var source = new CustomTextSource4(pixelsPerDip,rendering,new GenericTextRunProperties(rendering,pixelsPerDip), _f.Debugfn);
            source.SetText(code);
            var renderRequestInput = new RenderRequestInput(_codeControl,
                lineNo, offset, x, y,
                RoslynCodeControl.Formatter,
                width,pixelsPerDip,
                source,maxY,maxX,fontSize,
                fontFamilyName, 
                FontWeights.Regular);
            LineInfo2 curLineInfo = null;
            if(curLineInfoObjects != null)
            {
                curLineInfo = new LineInfo2((int) curLineInfoObjects[0],
                    (LinkedListNode<CharInfo>) curLineInfoObjects[1], (int) curLineInfoObjects[2],
                    new Point((double) curLineInfoObjects[3], (double) curLineInfoObjects[4]), (double) curLineInfoObjects[5],
                    (int) curLineInfoObjects[6]);
            }
            var result = RoslynCodeControl.RedrawLine(renderRequestInput,rendering,null,curLineInfo);
            var i = 0;
            foreach (var sourceRunInfo in source.Runs)
            {
                _f.Debugfn($"Run {i} : {sourceRunInfo}");
            }
            var dg = result.DrawingGroup;
            var l = result.LineInfo;
            i = 0;
            var c = l.FirstCharInfo;
            if (runIndiciesAry != null)
            {
                Assert.NotNull(c);
                Assert.NotNull(c.List);
                var ri = c.List.Select(c1 => c1.RunIndex);
                var chars = string.Join("", c.List.Select(c1 => c1.Character));
                _f.Debugfn("chars: " + chars);
                Assert.Equal(runIndiciesAry, ri);
                var runIndicies = "{" + string.Join(", ", ri) + "}";
                _f.Debugfn(runIndicies);
            }

            
            while (c != null)
            {
                Assert.NotNull(c.Value);
                _f.Debugfn($"CharInfo[{i}] {c.Value}");
                c = c.Next;
            }
            _f.Debugfn(l.ToString());
            Assert.Equal(lineNo, l.LineNumber);
            Assert.Equal(outLength, l.Length);
            Assert.Equal(offset, l.Offset);
            Assert.Equal(new Point(x,y), l.Origin);
            Assert.Equal(isNewLineInfo, result.IsNewLineInfo);
            Assert.Collection(dg.Children, drawing =>
            {
                Assert.IsType<DrawingGroup>(drawing);
                Assert.Collection(((DrawingGroup) drawing).Children,
                    Enumerable.Repeat<Action<Drawing>>(z =>
                    {
                        Assert.IsType<GlyphRunDrawing>(z);
                    }, dgCount).ToArray());

            });

        
        }
        [WpfFact]
        public void Test5_1()
        {
            var jt = _codeControl.JTF.RunAsync(DoInput1Async);
            var continueWith = jt.JoinAsync().ContinueWith(async t =>
            {
                if (_codeControl.IsFaulted) _f.OutputHelper.WriteLine("faulted");
                await _codeControl.ShutdownAsync();

                _f.OutputHelper.WriteLine("shut down secondary");
                _codeControl.SecondaryDispatcher.InvokeShutdown();
                _f.OutputHelper.WriteLine("DONE");
            }, CancellationToken.None, TaskContinuationOptions.None, TaskScheduler.FromCurrentSynchronizationContext());

            while (!jt.IsCompleted)
            {
                DoEvents();
                Thread.Sleep(0);
            }

            _f.OutputHelper.WriteLine("end of test");
        }


        [WpfFact]
        public void Test5()
        {
            _codeControl.JTF2 = _f.JTF2;
            _codeControl.SourceText = "";

            var w = new Window();
            _window = w;
            w.Content = _codeControl;

            JoinableTask jt1 = null;
            w.Loaded += (sender, args) => { jt1 = _codeControl.JTF.RunAsync(DoInput1Async); };

            try
            {
                w.ShowDialog();
            }
            catch (Exception ex)
            {
                _f.Debugfn(ex.ToString());
            }

            jt1.Join();
            if (_spans != null)
                foreach (var timeSpans in _spans)
                    _outputHelper.WriteLine(string.Join("\t", timeSpans.Select(z => z.TotalMilliseconds)));
        }

        [WpfFact]
        public void Test6()
        {
            Action<string> debugOut = (s) => _outputHelper.WriteLine(s);
            _codeControl.JTF2 = _f.JTF2;
            // c.JTF = new JoinableTaskFactory(new JoinableTaskContext());
            _codeControl.SourceText = File.ReadAllText(@"C:\temp\dockingmanager.cs");

            var w = new Window();
            _window = w;
            w.Content = _codeControl;
            var StartTime = DateTime.MinValue;
            _codeControl.AddHandler(RoslynCodeBase.RenderStartEvent, new RoutedEventHandler((sender, args) =>
            {
                StartTime = DateTime.Now;
                Debug.WriteLine("render start");
            }));
            _codeControl.AddHandler(RoslynCodeBase.RenderCompleteEvent, new RoutedEventHandler((sender, args) =>
            {
                var span = DateTime.Now - StartTime;
                var msg1 = "render complete " + span;
                Debug.WriteLine(msg1);
                _outputHelper.WriteLine(msg1);
            }));
            // w.Loaded += OnWOnLoaded2;
            _closeWindow = true;
            w.ShowDialog();
        }

#if false
[WpfFact]
        public void Test7()
        {
            Action<string> debugOut = (s) => _outputHelper.WriteLine(s);
            var c = new RoslynCodeBase(debugOut);
            _control0 = c;
            c.JTF2 = _f.JTF2;
            // c.JTF = new JoinableTaskFactory(new JoinableTaskContext());
            c.SourceText = File.ReadAllText(@"C:\temp\dockingmanager.cs");

            var StartTime = DateTime.MinValue;
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
#endif

        private async Task DoInput1Async()
        {
            var e = new EventSource("Test1");
            var cTempProgram0Cs = @"C:\temp\program.cs";
            var csFile = Environment.GetEnvironmentVariable("CSFILE");
            if (csFile != null) cTempProgram0Cs = csFile;
            var code = File.ReadAllLines(cTempProgram0Cs);
            var c2 = _codeControl;
            Debug.WriteLine("loaded");
            var timings = new List<TimeSpan>();
            var avgs = new List<double>();
            _spans = new List<List<TimeSpan>>();
            await c2.UpdateFormattedTextAsync();
            // var success2 = await c2.DoInputAsync(new InputRequest(InputRequestKind.NewLine));
            var now0 = DateTime.Now;
            var ip = 0;
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
                    _spans.Add(new List<TimeSpan>(5)
                    {
                        successRenderRequestTimestamp, postUpdateTimestamp, completedTimestamp,
                        successRenderBeganTimestamp, successRenderCompleteTimestamp
                    });
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
                        _outputHelper.WriteLine(averageIs);
                        MyCompanyEventSource.Log.Timing(avg);
                        avgs.Add(avg);
                    }
                }

                await c2.DoInputAsync(new InputRequest(InputRequestKind.NewLine));
            }

            if (_window != null)
            {
                var dpi = VisualTreeHelper.GetDpi(_window);
                var bmp = new RenderTargetBitmap((int) _window.ActualWidth, (int) _window.ActualHeight,
                    dpi.PixelsPerInchX, dpi.PixelsPerInchY, PixelFormats.Pbgra32);
                bmp.Render(_window);
                var zz = new PngBitmapEncoder();
                zz.Frames.Add(BitmapFrame.Create(bmp));
                using (var stream = new FileStream(@"c:\temp\window.png", FileMode.Create))
                {
                    zz.Save(stream);
                }
            }

            if (_closeWindow)
                _window?.Close();
        }

#if false
        private async void OnWOnLoaded2(object sender, RoutedEventArgs args)
        {
            var c2 = _control0;
            await c2.UpdateFormattedTextAsync();

            WriteDocument(c2);
            if (_closeWindow)
                _window.Close();
        }
#endif

        private static void WriteDocument(RoslynCodeBase b)
        {
            var bDocumentPaginator = b.DocumentPaginator;
            if (bDocumentPaginator == null) throw new InvalidOperationException("No paginator");
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


        [SecurityPermission(SecurityAction.Demand, Flags = SecurityPermissionFlag.UnmanagedCode)]
        public void DoEvents()
        {
            var frame = new DispatcherFrame();
            Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Background,
                new DispatcherOperationCallback(ExitFrame), frame);
            Dispatcher.PushFrame(frame);
        }

        public object ExitFrame(object f)
        {
            ((DispatcherFrame) f).Continue = false;

            return null;
        }
    }

    internal class TestContext
    {
        public int Length { get; set; }
    }
}