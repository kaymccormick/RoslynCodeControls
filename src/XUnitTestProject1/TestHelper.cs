using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.IO;
using System.Linq;
using System.Security.Permissions;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using System.Windows.Xps.Packaging;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host;
using RoslynCodeControls;
using Xunit;

namespace XUnitTestProject1
{
    internal static class TestHelper
    {
        public static async Task DoInput1Async(UnitTest1 unitTest1)
        {
            var e = new EventSource("Test1");
            var cTempProgram0Cs = @"C:\temp\program.cs";
            var csFile = Environment.GetEnvironmentVariable("CSFILE");
            if (csFile != null) cTempProgram0Cs = csFile;
            var code = File.ReadAllLines(cTempProgram0Cs);
            var c2 = unitTest1.CodeControl;
            Debug.WriteLine("loaded");
            var timings = new List<TimeSpan>();
            var avgs = new List<double>();
            var spans = new List<List<TimeSpan>>();
            await c2.UpdateFormattedTextAsync();
            // var success2 = await codeControl.DoInputAsync(new InputRequest(InputRequestKind.NewLine));
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
                    unitTest1.MyFixture.Debugfn("" + ch + " insertion point is " + c2.InsertionPoint);
                    var now1 = DateTime.Now;

                    var success = await c2.DoInputAsync(new InputRequest(InputRequestKind.TextInput, ch.ToString()));
                    ip++;
                    var successRenderRequestTimestamp = success.RenderRequestTimestamp - now1;
                    var postUpdateTimestamp = success.PostUpdateTimestamp - now1;
                    var completedTimestamp = success.Timestamp - now1;
                    var successRenderBeganTimestamp = success.RenderBeganTimestamp - now1;
                    var successRenderCompleteTimestamp = success.RenderCompleteTimestamp - now1;
                    spans.Add(new List<TimeSpan>(5)
                    {
                        successRenderRequestTimestamp, postUpdateTimestamp, completedTimestamp,
                        successRenderBeganTimestamp, successRenderCompleteTimestamp
                    });
                    unitTest1.MyFixture.Debugfn(successRenderRequestTimestamp.ToString());
                    unitTest1.MyFixture.Debugfn(postUpdateTimestamp.ToString());
                    unitTest1.MyFixture.Debugfn(completedTimestamp.ToString());
                    var elapsed = DateTime.Now - now1;

                    timings.Add(elapsed);
                    unitTest1.MyFixture.Debugfn("took " + elapsed);

                    if (timings.Count % 20 == 1)
                    {
                        var avg = timings.Average(span => span.TotalMilliseconds);
                        var averageIs = "Average is " + avg;
                        unitTest1.OutputHelper.WriteLine(averageIs);
                        MyCompanyEventSource.Log.Timing(avg);
                        avgs.Add(avg);
                    }
                }

                await c2.DoInputAsync(new InputRequest(InputRequestKind.NewLine));
            }

            if (unitTest1.Window != null)
            {
                var dpi = VisualTreeHelper.GetDpi(unitTest1.Window);
                var bmp = new RenderTargetBitmap((int) unitTest1.Window.ActualWidth, (int) unitTest1.Window.ActualHeight,
                    dpi.PixelsPerInchX, dpi.PixelsPerInchY, PixelFormats.Pbgra32);
                bmp.Render(unitTest1.Window);
                var zz = new PngBitmapEncoder();
                zz.Frames.Add(BitmapFrame.Create(bmp));
                using (var stream = new FileStream(@"c:\temp\window.png", FileMode.Create))
                {
                    zz.Save(stream);
                }
            }

            if (unitTest1.CloseWindow) unitTest1.Window?.Close();
        }

        public static async Task OnLoadedAsync(RoslynCodeControl codeControl, bool closeWindow, Window window)
        {
            for(var i = 0; i < 10; i++)
            {
                await codeControl.UpdateFormattedTextAsync();
                
                var msg = codeControl.LineInfos2.Count.ToString();
                codeControl.DebugFn(msg, 0);
                ProtoLogger.Instance.LogAction(msg);
            }

            // WriteDocument(codeControl);
            if (closeWindow)
            {
                window.Close();
            }
        }

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
        public static void DoEvents()
        {
            var frame = new DispatcherFrame();
            Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Background,
                new DispatcherOperationCallback(ExitFrame), frame);
            Dispatcher.PushFrame(frame);
        }

        public static object ExitFrame(object f)
        {
            ((DispatcherFrame) f).Continue = false;

            return null;
        }

        public static void DoInput(UnitTest1 unitTest1, string input, bool checkResult = true)
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

                unitTest1.MyFixture.Debugfn(done.ToString());
            };

            var jt = unitTest1.JTF.RunAsync(async () =>
            {
                await unitTest1.CodeControl.UpdateFormattedTextAsync();
                var context = new TestContext();
                var lines = input.Split("\r\n");
                foreach (var line in lines)
                {
                    unitTest1.MyFixture.Debugfn(line);
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

        public static Document SetupDocument(string Filename, HostServices hostServices)
        {
            AdhocWorkspace w;
            w = hostServices != null ? new AdhocWorkspace(hostServices) : new AdhocWorkspace();

            w.AddSolution(SolutionInfo.Create(SolutionId.CreateNewId(), VersionStamp.Create()));
            var projectInfo = ProjectInfo.Create(ProjectId.CreateNewId(), VersionStamp.Create(),
                "Code Project", "code", LanguageNames.CSharp);
            var w2 = w.CurrentSolution.AddProject(projectInfo);
            w.TryApplyChanges(w2);

            DocumentInfo documentInfo;
            var filename = Filename;
            if (filename != null)
                documentInfo = DocumentInfo.Create(DocumentId.CreateNewId(projectInfo.Id), "Default",
                    null, SourceCodeKind.Regular, new FileTextLoader(filename, Encoding.UTF8), filename);
            else
                documentInfo = DocumentInfo.Create(DocumentId.CreateNewId(projectInfo.Id), "Default",
                    null, SourceCodeKind.Regular);

            w2 = w.CurrentSolution.AddDocument(documentInfo);
            w.TryApplyChanges(w2);

            
            return  w.CurrentSolution.GetDocument(documentInfo.Id);
        }
    }
}