using System.Collections.Generic;
using System.Threading.Channels;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Threading;
using JetBrains.Annotations;
using Microsoft.CodeAnalysis;

namespace RoslynCodeControls
{
    public interface IFace1 : ICodeView
    {
        /* Drawing /rendering */
    }

    public interface ICodeView
    {
        Document Document { get; }
        string SourceText { get; set; }
        double MaxX { get; }
        double MaxY { get; }
        DrawingBrush DrawingBrush { get; }
        string DocumentTitle { get; set; }
        bool PerformingUpdate { get; set; }
        CodeControlStatus Status { get; set; }
        double XOffset { get; set; }
        DrawingGroup TextDestination { get; set; }
        FontFamily FontFamily { get; set; }
        double OutputWidth { get; set; }
        double FontSize { get; set; }
        FontWeight FontWeight { get; set; }
        Dispatcher SecondaryDispatcher { get; }
        double PixelsPerDip { get; set; }
        DispatcherOperation<CustomTextSource4> InnerUpdateDispatcherOperation { get; set; }
        Channel<UpdateInfo> UpdateChannel { get; set; }
        DocumentPaginator DocumentPaginator { get; }
        Dispatcher Dispatcher { get; }
        ScrollViewer _scrollViewer { get; set; }
        CustomTextSource4 CustomTextSource { get; set; }
        bool InitialUpdate { get; set; }
        int InsertionPoint { get; set; }
        CharInfo InsertionCharInfo { get; set; }
        LinkedList<CharInfo> CharInfos { get; set; }
        Task UpdateFormattedTextAsync();
        void RaiseEvent(RoutedEventArgs p0);
        TextSourceInitializationParameters CreateDefaultTextSourceArguments();
        Task<CustomTextSource4> InnerUpdate(MainUpdateParameters mainUpdateParameters, TextSourceInitializationParameters textSourceInitializationParameters);
    }

}