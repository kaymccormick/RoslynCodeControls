using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using JetBrains.Annotations;
using Microsoft.VisualStudio.Threading;
using RoslynCodeControls;
using Xunit;
using Xunit.Abstractions;

namespace XUnitTestProject1
{
    [UsedImplicitly]
    public class MyFixture : IAsyncLifetime
    {
        private ITestOutputHelper _outputHelper;

        /// <inheritdoc />
        public async Task InitializeAsync()
        {
            // string name = MyCompanyEventSource.GetName(typeof(MyCompanyEventSource));
            // IEnumerable<EventSource> eventSources = MyCompanyEventSource.GetSources();

            var mEvent = new ManualResetEvent(false);
            var startSecondaryThread = RoslynCodeControl.StartSecondaryThread(mEvent, (d) => { });

            await mEvent.ToTask();
            var d = Dispatcher.FromThread(startSecondaryThread);
            var jtf2 = new JoinableTaskFactory(new JoinableTaskContext(RoslynCodeControl.SecondaryThread,
                new DispatcherSynchronizationContext(d)));
            JTF2 = jtf2;
        }

        public void Debugfn(string msg)
        {
#if DEBUG
            var newMsg = Thread.CurrentThread.ManagedThreadId + ": " + Task.CurrentId + ": " + msg;
            Debug.WriteLine(newMsg);
            _outputHelper.WriteLine(newMsg);
#endif
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