using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.TextFormatting;
using System.Windows.Threading;
using JetBrains.Annotations;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace RoslynCodeControls    
{
    public static class CommonText
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="roslynCodeControl"></param>
        [ItemCanBeNull]
        public static async Task<DispatcherOperation> UpdateFormattedText(IFace1 roslynCodeControl)
        {
            Debug.WriteLine("Enteirng updateformattedtext " + roslynCodeControl.PerformingUpdate);
            if (roslynCodeControl.PerformingUpdate)
            {
                Debug.WriteLine("Already performing update");
                return null;
            }

            roslynCodeControl.PerformingUpdate = true;
            roslynCodeControl.Status = CodeControlStatus.Rendering;
            roslynCodeControl.RaiseEvent(new RoutedEventArgs(RoslynCodeControl.RenderStartEvent, roslynCodeControl));

            var textStorePosition = 0;
            var linePosition = new Point(roslynCodeControl.XOffset, 0);

            roslynCodeControl.TextDestination.Children.Clear();

            TextLineBreak prev = null;
            LineInfo prevLine = null;
            RegionInfo prevRegion = null;
            var line = 0;

            Debug.WriteLine("Calling inner update");
            // _scrollViewer.VerticalScrollBarVisibility = ScrollBarVisibility.Disabled;
            var fontFamilyFamilyName = roslynCodeControl.FontFamily.FamilyNames[XmlLanguage.GetLanguage("en-US")];
            Debug.WriteLine(fontFamilyFamilyName);
            Debug.WriteLine("OutputWidth " + roslynCodeControl.OutputWidth);
            // not sure what to do here !!
            // Rectangle.Width = OutputWidth + Rectangle.StrokeThickness * 2;
            var emSize = roslynCodeControl.FontSize;
            var fontWeight = roslynCodeControl.FontWeight;
            var customTextSource4Parameters = roslynCodeControl.CreateDefaultTextSourceArguments();
            var dispatcherOperation = roslynCodeControl.SecondaryDispatcher.InvokeAsync(() =>
            {
                return RoslynCodeControl.InnerUpdate(new MainUpdateParameters(textStorePosition, line, linePosition, RoslynCodeControl.Formatter, roslynCodeControl.OutputWidth, roslynCodeControl.PixelsPerDip, emSize, fontFamilyFamilyName, roslynCodeControl.UpdateChannel.Writer, fontWeight, roslynCodeControl.DocumentPaginator, customTextSource4Parameters));
            });
            roslynCodeControl.InnerUpdateDispatcherOperation = dispatcherOperation;
            var source = await dispatcherOperation.Task
                .ContinueWith(
                    task =>
                    {
                        if (task.IsFaulted)
                        {
                            var xx1 = task.Exception?.Flatten().ToString() ?? "";
                            Debug.WriteLine(xx1);
                            // ReSharper disable once PossibleNullReferenceException
                            Debug.WriteLine(task.Exception.ToString());
                        }

                        return task.Result;
                    }).ConfigureAwait(false);

            return roslynCodeControl.Dispatcher.InvokeAsync(() =>
            {
                roslynCodeControl.InnerUpdateDispatcherOperation = null;
                if (roslynCodeControl._scrollViewer != null) roslynCodeControl._scrollViewer.VerticalScrollBarVisibility = ScrollBarVisibility.Visible;
                roslynCodeControl.CustomTextSource = source;
                Debug.WriteLine("Return from await inner update");

                // ;(int.TryParse("Setting reactangle width to " +new NTComputer ({ff, line) || Device<int>()))};

                roslynCodeControl.PerformingUpdate = false;
                roslynCodeControl.InitialUpdate = false;
                roslynCodeControl.RaiseEvent(new RoutedEventArgs(RoslynCodeControl.RenderCompleteEvent, roslynCodeControl));
                roslynCodeControl.Status = CodeControlStatus.Rendered;
                var insertionPoint = roslynCodeControl.InsertionPoint;
                if (insertionPoint == 0) roslynCodeControl.InsertionCharInfo = roslynCodeControl.CharInfos.FirstOrDefault();
                CommandManager.InvalidateRequerySuggested();
            });
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="d"></param>
        /// <param name="pixelsPerDip"></param>
        /// <param name="tf"></param>
        /// <param name="st"></param>
        /// <param name="node"></param>
        /// <param name="compilation"></param>
        /// <param name="fontSize"></param>
        /// <param name="x"></param>
        /// <returns></returns>
        public static CustomTextSource4 CreateAndInitTextSource(double pixelsPerDip,
            Typeface tf, SyntaxTree st, SyntaxNode node, Compilation compilation, double fontSize)

        {
            if (st == null)
            {
                st = SyntaxFactory.ParseSyntaxTree("");
                node = st.GetRoot();
                compilation = null;
            }

            var textDecorationCollection = new TextDecorationCollection();
            var typeface = tf;
            var fontRendering = FontRendering.CreateInstance(fontSize,
                TextAlignment.Left, textDecorationCollection,
                Brushes.Black, typeface);
            var source = new CustomTextSource4(pixelsPerDip, fontRendering, new GenericTextRunProperties(
                fontRendering,
                pixelsPerDip))
            {
                EmSize = fontSize,
                Compilation = compilation,
                Tree = st,
                Node = node
            };
                //source.PropertyChanged += x;
            source.Init();
            return source;
        }
    }
}