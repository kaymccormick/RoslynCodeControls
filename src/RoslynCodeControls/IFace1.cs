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
    public interface IFace1
    {
        bool PerformingUpdate { get; set; }
        CodeControlStatus Status { get; set; }
        /* Drawing /rendering */
        double XOffset { get; set; }
        DrawingGroup TextDestination { get; set; }
        FontFamily FontFamily { get; set; }
        double OutputWidth { get; set; }
        double FontSize { get; set; }
        FontWeight FontWeight { get; set; }
        Dispatcher SecondaryDispatcher { get; }
        double PixelsPerDip { get; set; }
        DispatcherOperation<CustomTextSource4> InnerUpdateDispatcherOperation { get; set; }
        Channel<UpdateInfo>  UpdateChannel { get; set; }
        DocumentPaginator DocumentPaginator { get;  }
        Dispatcher Dispatcher { get;  }
        ScrollViewer _scrollViewer { get; set; }
        CustomTextSource4 CustomTextSource { get; set; }
        bool InitialUpdate { get; set; }
        int InsertionPoint { get; set; }
        CharInfo InsertionCharInfo { get; set; }
        void RaiseEvent(RoutedEventArgs p0);
        TextSourceInitializationParameters CreateDefaultTextSourceArguments();
        LinkedList<CharInfo> CharInfos { get; set; }
        [ItemCanBeNull] Task<CustomTextSource4> InnerUpdate(MainUpdateParameters mainUpdateParameters, TextSourceInitializationParameters textSourceInitializationParameters);
    }

    public interface CodeIface
    {
        Document Document { get; }
        double OutputWidth { get; set; }
        string SourceText { get; set; }
        Task UpdateFormattedTextAsync();
    }

}