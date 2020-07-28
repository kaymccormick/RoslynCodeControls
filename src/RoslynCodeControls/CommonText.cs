using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.TextFormatting;
using System.Windows.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using TextLine = System.Windows.Media.TextFormatting.TextLine;
// ReSharper disable ConvertToUsingDeclaration
#pragma warning disable 8600
#pragma warning disable 8625

namespace RoslynCodeControls
{
    public static class CommonText
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="iface1"></param>
        /// <param name="source"></param>
#if false
        [ItemCanBeNull]
        public static async Task<CustomTextSource4> UpdateFormattedText(ICodeView iface1)
        {
            Debug.WriteLine("Enteirng updateformattedtext " + iface1.PerformingUpdate);
            if (iface1.PerformingUpdate)
            {
                Debug.WriteLine("Already performing update");
                return null;
            }

            iface1.PerformingUpdate = true;
            iface1.Status = CodeControlStatus.Rendering;
            iface1.RaiseEvent(new RoutedEventArgs(RoslynCodeControl.RenderStartEvent, iface1));

            var textStorePosition = 0;
            var linePosition = new Point(iface1.XOffset, 0);

            iface1.TextDestination.Children.Clear();

            var line = 0;

            Debug.WriteLine("Calling inner update");
            // ScrollViewer.VerticalScrollBarVisibility = ScrollBarVisibility.Disabled;
            var fontFamilyFamilyName = iface1.FontFamily.FamilyNames[XmlLanguage.GetLanguage("en-US")];
            Debug.WriteLine(fontFamilyFamilyName);
            Debug.WriteLine("OutputWidth " + iface1.OutputWidth);
            // not sure what to do here !!
            // Rectangle.Width = OutputWidth + Rectangle.StrokeThickness * 2;
            var emSize = iface1.FontSize;
            var fontWeight = iface1.FontWeight;
            var customTextSource4Parameters = iface1.CreateDefaultTextSourceArguments();
            var mainUpdateParameters = new MainUpdateParameters(textStorePosition, line, linePosition,
                RoslynCodeControl.Formatter, iface1.OutputWidth, iface1.PixelsPerDip, emSize, fontFamilyFamilyName,
                iface1.UpdateChannel.Writer, fontWeight, iface1.DocumentPaginator, customTextSource4Parameters);
            await iface1.JTF2.SwitchToMainThreadAsync();
            
                var source = await iface1.InnerUpdateAsync(mainUpdateParameters, customTextSource4Parameters);

            return source;
        }
#endif
        public static DispatcherOperation MainUpdateContinuation(ICodeView iface1, CustomTextSource4 source)
        {
            return iface1.Dispatcher.InvokeAsync(() =>
            {
                iface1.CustomTextSource = source;
                Debug.WriteLine("Return from await inner update");

                // ;(int.TryParse("Setting reactangle width to " +new NTComputer ({ff, line) || Device<int>()))};

                iface1.PerformingUpdate = false;
                iface1.InitialUpdate = false;
                iface1.RaiseEvent(new RoutedEventArgs(RoslynCodeBase.RenderCompleteEvent, iface1));
                iface1.Status = CodeControlStatus.Rendered;
                var insertionPoint = iface1.InsertionPoint;
                if (insertionPoint == 0) iface1.InsertionCharInfo = iface1.CharInfos.FirstOrDefault();
            }, DispatcherPriority.Send);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="pixelsPerDip"></param>
        /// <param name="tf"></param>
        /// <param name="st"></param>
        /// <param name="node"></param>
        /// <param name="compilation"></param>
        /// <param name="fontSize"></param>
        /// <param name="pDebugFn"></param>
        /// <returns></returns>
        public static CustomTextSource4 CreateAndInitTextSource(double pixelsPerDip,
            Typeface tf, SyntaxTree st, SyntaxNode node, Compilation compilation, double fontSize,
            RoslynCodeBase.DebugDelegate pDebugFn)

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
            var genericTextRunProperties = new GenericTextRunProperties(
                fontRendering,
                pixelsPerDip);
            var source = new CustomTextSource4(pixelsPerDip, fontRendering, genericTextRunProperties, pDebugFn)
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

        public static unsafe CustomTextSource4 InnerUpdateAsync(MainUpdateParameters mainUpdateParameters,
            Func<CustomTextSource4> makeSource)
        {
            LinkedListNode<CharInfo> charInfoNode = null;
            const int batchLines = 100;
            var lineInfos0 = new LineInfo2[batchLines];
            var lineInfos1 = new LineInfo2[batchLines];
            var lineInfos = lineInfos0;
            int @switch = 0;
            mainUpdateParameters.DebugFn?.Invoke($"{nameof(InnerUpdateAsync)}", 5);
            var tf = CreateTypeface(new FontFamily(mainUpdateParameters.FaceName), FontStyles.Normal,

                FontStretches.Normal,
                mainUpdateParameters.FontWeight);

            var currentRendering = FontRendering.CreateInstance(mainUpdateParameters.FontSize,
                TextAlignment.Left,
                null,
                Brushes.Black,
                tf);

            var customTextSource4 = makeSource();

            var myGroup = new DrawingGroup();
            var myDc = myGroup.Open();

            var genericTextParagraphProperties =
                new GenericTextParagraphProperties(currentRendering, mainUpdateParameters.PixelsPerDip);
            var runsInfos = new List<TextRunInfo>();
            var allCharInfos = new LinkedList<CharInfo>();
            var textStorePosition = mainUpdateParameters.TextStorePosition;
            var lineNo = mainUpdateParameters.LineNo;
            // var tasks = new List<Task>();
            var linePosition = mainUpdateParameters.LinePosition;
            var liIndex = 0;
            while (textStorePosition < customTextSource4.Length)
            {
                var runCount = customTextSource4.Runs.Count;
#if DEBUGTEXTSOURCE
                Debug.WriteLine("Runcount = " + runCount);
#endif
                var numPages = 0;
                var pageBegin = new Point(0, 0);
                var pageEnd = new Point(0, 0);
                if (mainUpdateParameters.Paginate)
                    // ReSharper disable once PossibleInvalidOperationException
                    pageEnd.Offset(mainUpdateParameters.PageSize.Value.Width,
                        mainUpdateParameters.PageSize.Value.Height);

                LineInfo2 lineInfo;
                var linePositionX = mainUpdateParameters.ParagraphWidth - linePosition.X;
                if (linePositionX < 0)
                {
                    linePositionX = 0;
                }
                using (var myTextLine = mainUpdateParameters.TextFormatter.FormatLine(customTextSource4,
                    textStorePosition, linePositionX,
                    genericTextParagraphProperties,
                    null))
                {
                    
                    var nRuns = customTextSource4.Runs.Count - runCount;
                    if (nRuns < 0)
                    {
                        throw new InvalidOperationException("NRuns is negative" + nRuns);
                    }
#if DEBUGTEXTSOURCE
                    Debug.WriteLine("num runs for line is "  + nRuns);
#endif

                    var curCharInfoNode = charInfoNode;
                    HandleLine(allCharInfos, linePosition, myTextLine, customTextSource4, runCount,
                        nRuns, lineNo, textStorePosition, runsInfos,curCharInfoNode,out charInfoNode, 
                        mainUpdateParameters.DebugFn);
                    myTextLine.Draw(myDc, linePosition, InvertAxes.None);

                    lineInfos[liIndex++] = new LineInfo2(lineNo, curCharInfoNode != null? curCharInfoNode.Next : allCharInfos.First, textStorePosition, linePosition, myTextLine.Height,
                        myTextLine.Length);
                    linePosition.Y += myTextLine.Height;
                    lineNo++;

                    // Update the index position in the text store.
                    textStorePosition += myTextLine.Length;
                    if (mainUpdateParameters.Paginate && linePosition.Y >= pageEnd.Y)
                    {
                        Debug.WriteLine($"numPages: {numPages}");
                        numPages++;
                    }
                }

#if GROUPEDDG
                if (lineNo > 0 && lineNo % batchLines == 0)
                {
                    myDc.Close();
                    myGroup.Freeze();
                    var curUi = new UpdateInfo() {DrawingGroup = myGroup, CharInfos = allCharInfos.ToList<CharInfo>()};
                    await mainUpdateParameters.ChannelWriter.WriteAsync(curUi);
                    // tasks.Add(writeAsync.AsTask());
                    myGroup = new DrawingGroup();
                    myDc = myGroup.Open();
                }
#else
                if (lineNo <= 0 || lineNo % batchLines != 0) continue;
                myDc.Close();
                myGroup.Freeze();
                var curUi = new UpdateInfo(
                    myGroup,
                    liIndex,
                    lineInfos
                );
                liIndex = 0;
                if (@switch == 0)
                {
                    lineInfos = lineInfos1;
                    @switch = 1;
                }
                else
                {
                    lineInfos = lineInfos0;
                    @switch = 0;
                }
                mainUpdateParameters.ChannelWriter.WriteAsync(curUi);
                myGroup = new DrawingGroup();
                myDc = myGroup.Open();
                
#endif
            }

            customTextSource4.RunInfos = runsInfos;
#if GROUPEDDG
            if (lineNo % 100 != 0)
            {
                myDc.Close();
                myGroup.Freeze();
                var curUi = new UpdateInfo()
                    {DrawingGroup = myGroup, CharInfos = Enumerable.ToList<CharInfo>(allCharInfos), FinalBlock = true};
                await mainUpdateParameters.ChannelWriter.WriteAsync(curUi);
                // tasks.Add(writeAsync.AsTask());
            }
            else
            {
                myDc.Close();
            }
#else

            if (lineNo % batchLines == 0) return customTextSource4;
            {
                myDc.Close();
                myGroup.Freeze();
                var curUi = new UpdateInfo(
                    myGroup,
                    liIndex,
                    lineInfos
                );
                mainUpdateParameters.ChannelWriter.WriteAsync(curUi);
            }
#endif
            // await Task.WhenAll(tasks);

            return customTextSource4;
        }

        public static void HandleLine(LinkedList<CharInfo> allCharInfos, Point linePosition, TextLine myTextLine,
            CustomTextSource4 customTextSource4, int runCount, int nRuns, int lineNo, int textStorePosition,
            List<TextRunInfo> runsInfos,
             LinkedListNode<CharInfo> curCharInfoNode,
            out LinkedListNode<CharInfo> lastCharInfoNode,
            RoslynCodeBase.DebugDelegate? debugFn = null, TextChange? change = null,
            LineInfo2? curLineInfo=null)
        {
            lastCharInfoNode = null;
            var curPos = linePosition;
            // var positions = new List<Rect>();
            var indexedGlyphRuns = myTextLine.GetIndexedGlyphRuns();

            var runs = customTextSource4.Runs;
            
            debugFn?.Invoke($"Runs is count is {runs.Count}", 1);
            var textRuns = runs.Skip(runCount).Take(nRuns).ToList();
            debugFn?.Invoke($"{runCount} {nRuns} TextRuns enumerator count is {textRuns.Count}", 1);
            var curCi = curCharInfoNode;
            using (var enum1 = textRuns.GetEnumerator())
            {
                var moveNext = enum1.MoveNext();
                var lineCharIndex = 0;
                var xOrigin = linePosition.X;
                if (indexedGlyphRuns == null) return;
                foreach (var glyphRunC in indexedGlyphRuns)
                {
                    var gl = glyphRunC.GlyphRun;
                    var advanceSum = gl.AdvanceWidths.Sum();
                    var startCi = curCi;
                    for (var i = 0; i < gl.Characters.Count; i++)
                    {
                        var i0 = gl.ClusterMap?[i] ?? i;
                        var glAdvanceWidth = gl.AdvanceWidths[i0];
                        var glCharacter = gl.Characters[i];
                        var glCaretStop = gl.CaretStops?[i0];
                        if (curCi != null && curLineInfo!=null)
                        {
                            // ReSharper disable once PossibleInvalidOperationException
#pragma warning disable 8629
                            if (lineCharIndex + curLineInfo.Offset >= change.Value.Span.Start)
#pragma warning restore 8629
                            {
                                curCi.Value.Index = textStorePosition + lineCharIndex;
                                curCi.Value.RunIndex = i;
                                curCi.Value.Character = glCharacter;
                                curCi.Value.AdvanceWidth = glAdvanceWidth;
                                curCi.Value.XOrigin = xOrigin;
                                if(Math.Abs(curCi.Value.YOrigin - linePosition.Y) > 0.5)
                                    curCi.Value.YOrigin = linePosition.Y;
                            }
                            if(curCi.Value.RunIndex != i)
                                curCi.Value.RunIndex = i;
                            curCi = curCi.Next;
                        }
                        else
                        {
                            var ci = new CharInfo(lineNo, textStorePosition + lineCharIndex, lineCharIndex, i,
                                glCharacter, glAdvanceWidth,
                                glCaretStop, xOrigin, linePosition.Y);
                            allCharInfos.AddLast(ci);
                        }

                        lineCharIndex++;
                        xOrigin += glAdvanceWidth;
                    }

                    var item = new Rect(curPos, new Size(advanceSum, myTextLine.Height));
                    if(!moveNext)
                    {
                        StringBuilder sb = new StringBuilder();
                        for (var c = curCharInfoNode; c != null; c = c.Next)
                        {
                            sb.Append(c.Value.Character);
                        }

                        throw new CodeControlException("enumerator empty " + sb);
                    }

                    var enum1Current = enum1.Current;
                    var textRunInfo = new TextRunInfo(enum1Current, item, startCi);
                    debugFn?.Invoke(textRunInfo.ToString(), 2);
                    runsInfos?.Add(textRunInfo);

                    curPos.X += advanceSum;
                }

            }

#pragma warning disable 8601
            lastCharInfoNode = allCharInfos.Last;
#pragma warning restore 8601
        }

        private static Typeface CreateTypeface(FontFamily fontFamily, FontStyle fontStyle, FontStretch fontStretch,
            FontWeight fontWeight)
        {
            return new Typeface(fontFamily, fontStyle, fontWeight, fontStretch);
        }

        public static TextFormatter Formatter { get; } = TextFormatter.Create();
    }
}