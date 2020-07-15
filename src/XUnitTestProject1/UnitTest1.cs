using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using RoslynCodeControls;
using Xunit;

namespace XUnitTestProject1
{
    public class UnitTest1
    {
        [WpfFact]
        public void Test2()
        {
            var newMethod = NewMethod();
            newMethod.Wait();
        }

        private static async Task NewMethod()
        {
            RoslynCodeControl.StartSecondaryThread();
            RoslynCodeBase b = new RoslynCodeBase();
            CodeIface c = b;
            c.SourceText = "class T {}";
            var updateFormattedTextAsync = c.UpdateFormattedTextAsync();
            await updateFormattedTextAsync;
            
        }

        [WpfFact]
        public void Test1()
        {
            RoslynCodeControl.StartSecondaryThread();
            RoslynCodeControl rcc = new RoslynCodeControl();
            
            TaskCompletionSource<bool> taskCompletion = new TaskCompletionSource<bool>();
            rcc.AddHandler(RoslynCodeControl.RenderStartEvent, new RoutedEventHandler((sender, args) =>
                {
                    rcc.AddHandler(RoslynCodeControl.RenderCompleteEvent,
                        new RoutedEventHandler((sender, args) =>
                        {
                            Debug.WriteLine("Render complete");

                            taskCompletion.SetResult(true);
                        }));
                }
            ));
            rcc.Filename = @"C:\temp\program2.cs";
            CommonText.UpdateFormattedText(rcc).Wait();

            Assert.True(taskCompletion.Task.Wait(new TimeSpan(0, 0, 0, 5)));
        }

    }
}
