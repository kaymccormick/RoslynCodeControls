using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using Microsoft.Build.Locator;
using Microsoft.VisualStudio.Threading;
using RoslynCodeControls;

namespace WpfTestApp
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    ///
    /// 

    public partial class App : Application
    {
        /// <inheritdoc />
        public App()
        {
            
            MSBuildLocator.RegisterDefaults();
        }

        private Thread t2;
        private ManualResetEvent _mevent;
        private string _file;

        /// <inheritdoc />
        protected override void OnStartup(StartupEventArgs e)
        {
            var args = e.Args.ToList();
            if(args.Any())
            {
                string cmd = string.Empty;
                if (args.First().StartsWith("-"))
                {
                    cmd = args.First();
                    args = args.Skip(1).ToList();
                }

                if (args.Any())
                {
                    _file = args.First();
                
                }

                if (!string.IsNullOrEmpty(cmd) && Equals(cmd, "-print"))
                {
                    StartupCommand = ApplicationCommands.Print;
                }
                
            }

            // Window w = new Window();
            // w.Content = new NugetSearch();
            // w.ShowDialog();

            _mevent = new ManualResetEvent(false);
            t2 = RoslynCodeControls.RoslynCodeControl.StartSecondaryThread(_mevent, null);
            JoinableTaskFactory f = new JoinableTaskFactory(new JoinableTaskContext());
            f.RunAsync(Z);
            base.OnStartup(e);
        }

        public ICommand StartupCommand { get; set; }

        private async Task Z()
        {
            await _mevent.ToTask();
            var d = Dispatcher.FromThread(t2);
            var jtf2 = new JoinableTaskFactory(new JoinableTaskContext(RoslynCodeControl.SecondaryThread,
                new DispatcherSynchronizationContext(d)));
            MainWindow w = new MainWindow();
            w.StartupCommand = StartupCommand;
            w.Filename = _file;
            w.JTF2 = jtf2;
            w.Show();
        }

        private void App_OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            Debug.WriteLine(e.Exception.ToString());
            MessageBox.Show(e.Exception.ToString(), "Error");
            Application.Current.Shutdown(1);
        }
    }

}
