using System.Collections.Generic;
using System.Threading.Channels;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.Threading;

namespace RoslynCodeControls
{
    public interface ICodeView
    {
        LineInfo2? InsertionLine { get; }
        Rectangle Rectangle { get; }
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
        
        Channel<UpdateInfo> UpdateChannel { get; set; }
        DocumentPaginator DocumentPaginator { get; }
        Dispatcher Dispatcher { get; }
        ScrollViewer ScrollViewer { get; set; }
        CustomTextSource4 CustomTextSource { get; set; }
        bool InitialUpdate { get; set; }
        int InsertionPoint { get; set; }
        CharInfo InsertionCharInfo { get; set; }
        LinkedList<CharInfo> CharInfos { get; set; }
        JoinableTaskFactory JTF2 { get; set; }
        LinkedListNode<LineInfo2> InsertionLineNode { get; set; }
        Task UpdateFormattedTextAsync();
        void RaiseEvent(RoutedEventArgs p0);
        TextSourceInitializationParameters CreateDefaultTextSourceArguments();
        CustomTextSource4 InnerUpdate(MainUpdateParameters mainUpdateParameters, TextSourceInitializationParameters textSourceInitializationParameters);
        LinkedListNode<LineInfo2> FindLine(int lineNo, LinkedListNode<LineInfo2> startNode = null);
        void Reset();
    }
}