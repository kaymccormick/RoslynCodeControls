using System;
using System.Diagnostics;
using System.Windows.Threading;
using Microsoft.CodeAnalysis.MSBuild;

namespace WpfTestApp
{
    internal class Progress1 : IProgress<ProjectLoadProgress>
    {
        public MainWindow CodeWindow { get; }

        public Progress1(MainWindow codeWindow)
        {
            CodeWindow = codeWindow;
        }

        /// <inheritdoc />
        public void Report(ProjectLoadProgress value)
        {
            Debug.WriteLine(value.FilePath);
            CodeWindow.Dispatcher.Invoke(() =>
            {
                CodeWindow.status.Text +=
                    $"{value.Operation}: {value.TargetFramework}: {value.ElapsedTime}: {value.FilePath}\r\n\r\n";
                CodeWindow.StatusScrollViewer.ScrollToBottom();
            }, DispatcherPriority.Send);
        }
    }
}