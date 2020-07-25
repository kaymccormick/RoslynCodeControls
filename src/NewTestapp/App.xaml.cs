using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using Microsoft.VisualStudio.Threading;
using RoslynCodeControls;

namespace NewTestapp
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private Thread t2;
        private ManualResetEvent _mevent;
        private string _file;

        /// <inheritdoc />
        protected override void OnStartup(StartupEventArgs e)
        {
            if (e.Args.Any())
            {
                _file = e.Args.First();
            }
            _mevent = new ManualResetEvent(false);
            t2 = RoslynCodeControl.StartSecondaryThread(_mevent);
            JoinableTaskFactory f = new JoinableTaskFactory(new JoinableTaskContext());
            f.RunAsync(Z);
            base.OnStartup(e);
        }

        private async Task Z()
        {
            await _mevent.ToTask();
            var d = Dispatcher.FromThread(t2);
            var jtf2 = new JoinableTaskFactory(new JoinableTaskContext(RoslynCodeControl.SecondaryThread,
                // ReSharper disable once AssignNullToNotNullAttribute
                new DispatcherSynchronizationContext(d)));
            MainWindow w = new MainWindow();
            w.Filename = _file;
            w.JTF2 = jtf2;
            w.Show();
        }
    }

}
