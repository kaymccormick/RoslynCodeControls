using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace NewTestapp
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        /// <inheritdoc />
        protected override void OnStartup(StartupEventArgs e)
        {
            ManualResetEvent mevent= new ManualResetEvent(false);
            RoslynCodeControls.RoslynCodeControl.StartSecondaryThread(mevent,null);
            base.OnStartup(e);
        }
    }
}
