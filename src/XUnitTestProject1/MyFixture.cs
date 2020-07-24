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
        private JoinableTaskCollection _coll;
        private Dispatcher _d;

        public MyFixture(in int debugLevel)
        {
            DebugLevel = debugLevel;
        }

        /// <inheritdoc />
        public async Task InitializeAsync()
        {
            // string name = MyCompanyEventSource.GetName(typeof(MyCompanyEventSource));
            // IEnumerable<EventSource> eventSources = MyCompanyEventSource.GetSources();

            var mEvent = new ManualResetEvent(false);
            var startSecondaryThread = RoslynCodeControl.StartSecondaryThread(mEvent, (d) => { });

            await mEvent.ToTask();
            _d = Dispatcher.FromThread(startSecondaryThread);
            var joinableTaskContext = new JoinableTaskContext(RoslynCodeControl.SecondaryThread,
                new DispatcherSynchronizationContext(_d));
            _coll = joinableTaskContext.CreateCollection();
            var jtf2 = joinableTaskContext.CreateFactory(_coll);
            JTF2 = jtf2;
        }

        public void Debugfn(string msg, int debugLevel=10)
        {
            if (DebugLevel < debugLevel) return;
#if DEBUG
            var newMsg = Thread.CurrentThread.ManagedThreadId + ": " + Task.CurrentId + ": " + msg;
            Debug.WriteLine(newMsg);
            _outputHelper.WriteLine(newMsg);
#endif
        }

        public int DebugLevel { get; }

        public JoinableTaskFactory JTF2 { get; set; }

        public ITestOutputHelper OutputHelper
        {
            get { return _outputHelper; }
            set { _outputHelper = value; }
        }

        /// <inheritdoc />
        public async Task DisposeAsync()
        {
            await _coll.JoinTillEmptyAsync();
            if (JTF2.Context.MainThread.IsAlive)
            {
                _d.InvokeShutdown();
            }
        }
    }
}