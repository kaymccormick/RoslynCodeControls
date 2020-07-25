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
using WpfTestApp;
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

        public MyFixture MyFixture
        {
            get { return _f; }
        }

        public ITestOutputHelper OutputHelper
        {
            get { return _outputHelper; }
        }

        public Window Window
        {
            get { return _window; }
        }

        public bool CloseWindow
        {
            get { return _closeWindow; }
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
            NewMethod(this, "a");
        }

        [WpfFact]
        public void TestDoInputAsync2()
        {
            NewMethod(this, "ab");
        }

        [WpfFact]
        public void TestDoInputAsync3()
        {
            var path =
                "C:\\Users\\mccor.LAPTOP-T6T0BN1K\\source\\repos\\KayMcCormick.Dev\\src\\RoslynCodeControls\\src\\XUnitTestProject1\\UnitTest1.cs";
            var code = File.ReadAllText(
                path);
            NewMethod(this, code, false);
        }


        private static void NewMethod(UnitTest1 unitTest1, string input, bool checkResult = true)
        {
            var insertionPoint = 0;


            Func<RoslynCodeControl, string, TestContext, Task> a = async (rcc, inputChar, context) =>
            {
                InputRequest ir;
                ir = inputChar == "\r\n"
                    ? new InputRequest(InputRequestKind.NewLine)
                    : new InputRequest(InputRequestKind.TextInput, inputChar);

                var done = await rcc.DoUpdateTextAsync(insertionPoint, ir);
                if (checkResult)
                {
                    Assert.Equal(inputChar, done.InputRequest.Text);

                    Assert.Single(rcc.LineInfos2);
                    var il = rcc.InsertionLine;
                    Assert.Equal(il, rcc.LineInfos2.First.Value);
                    Assert.Equal(0, il.Offset);
                    Assert.Equal(context.Length + 3, il.Length);
                    Assert.Equal(0, il.LineNumber);
                    Assert.Equal(new Point(0, 0), il.Origin);
                    var ci = il.FirstCharInfo;
                }

                unitTest1._f.Debugfn(done.ToString());
            };

            var jt = unitTest1.JTF.RunAsync(async () =>
            {
                await unitTest1.CodeControl.UpdateFormattedTextAsync();
                var context = new TestContext();
                var lines = input.Split("\r\n");
                foreach (var line in lines)
                {
                    unitTest1._f.Debugfn(line);
                    foreach (var ch in line)
                    {
                        await a(unitTest1.CodeControl, ch.ToString(), context);
                        context.Length++;
                    }

                    await a(unitTest1.CodeControl, "\r\n", context);
                    context.Length += 2;
                }
            });

            var continueWith = jt.JoinAsync().ContinueWith(unitTest1.ContinuationFunction);
            continueWith.ContinueWith(t =>
            {
                unitTest1.CodeControl.Shutdown();
                return t.Result;
            });

            //, CancellationToken.None, TaskContinuationOptions.None, TaskScheduler.FromCurrentSynchronizationContext());

            while (!continueWith.IsCompleted)
            {
                TestHelper.DoEvents();
                Thread.Sleep(500);
                unitTest1.MyFixture.Debugfn("loop");
            }


#pragma warning disable VSTHRD002 // Avoid problematic synchronous waits
            if (checkResult)
                Assert.True(continueWith.Result);
#pragma warning restore VSTHRD002 // Avoid problematic synchronous waits

            var s = unitTest1.CodeControl.CustomTextSource;
            if (!checkResult)
                return;
            Assert.Collection(s.Runs, run => { Assert.IsType<SyntaxTokenTextCharacters>(run); },
                run => { Assert.IsType<CustomTextEndOfParagraph>(run); });
            Assert.Collection(s.RunInfos, runInfo => { Assert.IsType<SyntaxTokenTextCharacters>(runInfo.TextRun); });
        }

        public bool ContinuationFunction(Task t)
        {
            if (t.IsFaulted)
            {
                _f.Debugfn("Task faulted");
                return false;
            }

            if (_codeControl.IsFaulted) _f.OutputHelper.WriteLine("faulted");
            return true;
        }

        [WpfTheory]
        [InlineData("", 0, 0, 0, 0, 0, 1, 0, 0, 12, "Arial", null, 2, true, 0, null)]
        [InlineData("test", 0, 0, 0, 0, 0, 1, 0, 0, 12, "Arial", null, 6, true, 1, new[] {0, 1, 2, 3})]
        [InlineData("foo\r\nbar", 0, 0, 0, 0, 0, 1, 0, 0, 12, "Arial", null, 5, true, 1, new[] {0, 1, 2})]
        [InlineData("public class A { }\r\n", 0, 0, 0, 0, 0, 1, 0, 0, 12, "Arial", null, 20, true, 9,
            new[] {0, 1, 2, 3, 4, 5, 0, 0, 1, 2, 3, 4, 0, 0, 0, 0, 0, 0})]
        [InlineData("public class A { }\r\n", 0, 0, 0, 0, 0, 1, 0, 0, 12, "Arial",
            new object[] {0, null, 0, 0.0, 0.0, 0.0, 0, 0}, 20, true, 9,
            new[] {0, 1, 2, 3, 4, 5, 0, 0, 1, 2, 3, 4, 0, 0, 0, 0, 0, 0})]
        public void TestRedrawLine1(string code, int lineNo, int offset, double x, double y, double width,
            double pixelsPerDip, double maxX, double maxY, double fontSize, string fontFamilyName,
            object[] curLineInfoObjects, int outLength, bool isNewLineInfo, int dgCount, int[] runIndiciesAry)
        {
            var face = new Typeface(new FontFamily(fontFamilyName), FontStyles.Normal, FontWeights.Normal,
                FontStretches.Normal);

            var rendering =
                FontRendering.CreateInstance(fontSize, TextAlignment.Left, new TextDecorationCollection(),
                    Brushes.Black, face);

            var source = new CustomTextSource4(pixelsPerDip, rendering,
                new GenericTextRunProperties(rendering, pixelsPerDip), _f.Debugfn);
            source.SetText(code);
            var renderRequestInput = new RenderRequestInput(_codeControl,
                lineNo, offset, x, y,
                RoslynCodeControl.Formatter,
                width, pixelsPerDip,
                source, maxY, maxX, fontSize,
                fontFamilyName,
                FontWeights.Regular);
            LineInfo2 curLineInfo = null;
            if (curLineInfoObjects != null)
                curLineInfo = new LineInfo2((int) curLineInfoObjects[0],
                    (LinkedListNode<CharInfo>) curLineInfoObjects[1], (int) curLineInfoObjects[2],
                    new Point((double) curLineInfoObjects[3], (double) curLineInfoObjects[4]),
                    (double) curLineInfoObjects[5],
                    (int) curLineInfoObjects[6]);
            var result = RoslynCodeControl.RedrawLine(renderRequestInput, rendering, null, curLineInfo);
            var i = 0;
            foreach (var sourceRunInfo in source.Runs) _f.Debugfn($"Run {i} : {sourceRunInfo}");
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
            Assert.Equal(new Point(x, y), l.Origin);
            Assert.Equal(isNewLineInfo, result.IsNewLineInfo);
            Assert.Collection(dg.Children, drawing =>
            {
                Assert.IsType<DrawingGroup>(drawing);
                Assert.Collection(((DrawingGroup) drawing).Children,
                    Enumerable.Repeat<Action<Drawing>>(z => { Assert.IsType<GlyphRunDrawing>(z); }, dgCount).ToArray());
            });
        }

        [WpfFact]
        public void Test5_1()
        {
            var jt = _codeControl.JTF.RunAsync(() => TestHelper.DoInput1Async(this));
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
                TestHelper.DoEvents();
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
            w.Loaded += (sender, args) => { jt1 = _codeControl.JTF.RunAsync(() => TestHelper.DoInput1Async(this)); };

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
            var roslynCodeControl =CodeControl;
            roslynCodeControl.JTF2 = _f.JTF2;
            // c.JTF = new JoinableTaskFactory(new JoinableTaskContext());
            var path =
                "C:\\Users\\mccor.LAPTOP-T6T0BN1K\\source\\repos\\KayMcCormick.Dev\\src\\RoslynCodeControls\\src\\XUnitTestProject1\\UnitTest1.cs";

            roslynCodeControl.SourceText = File.ReadAllText(path);

            var w = new Window();
            _window = w;
            w.Content = roslynCodeControl;
            var StartTime = DateTime.MinValue;
            roslynCodeControl.AddHandler(RoslynCodeBase.RenderStartEvent, new RoutedEventHandler((sender, args) =>
            {
                StartTime = DateTime.Now;
                Debug.WriteLine("render start");
            }));
            roslynCodeControl.AddHandler(RoslynCodeBase.RenderCompleteEvent, new RoutedEventHandler((sender, args) =>
            {
                var span = DateTime.Now - StartTime;
                var msg1 = "render complete " + span;
                Debug.WriteLine(msg1);
                OutputHelper.WriteLine(msg1);
            }));
            w.Loaded += (sender1, args1) => TestHelper.OnWOnLoaded2(this, sender1, args1);
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

        [WpfFact]
        public void TestAnalayzer1()
        {
            AppViewModel v = new AppViewModel();
            List<object> z= new List<object>();
            Assert.Collection(z);
        }
    }
}