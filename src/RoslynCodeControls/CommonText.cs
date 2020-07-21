using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.TextFormatting;
using System.Windows.Threading;
using JetBrains.Annotations;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Threading;
using TextLine = System.Windows.Media.TextFormatting.TextLine;

namespace RoslynCodeControls
{
    public static class CommonText
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="iface1"></param>
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
            // _scrollViewer.VerticalScrollBarVisibility = ScrollBarVisibility.Disabled;
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
                iface1.RaiseEvent(new RoutedEventArgs(RoslynCodeControl.RenderCompleteEvent, iface1));
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
        /// <param name="d"></param>
        /// <param name="x"></param>
        /// <returns></returns>
        public static CustomTextSource4 CreateAndInitTextSource(double pixelsPerDip,
            Typeface tf, SyntaxTree st, SyntaxNode node, Compilation compilation, double fontSize,
            Action<string> pDebugFn)

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

        public static async Task<CustomTextSource4> InnerUpdateAsync(MainUpdateParameters mainUpdateParameters,
            Func<CustomTextSource4> makeSource)
        {
            mainUpdateParameters.DebugFn?.Invoke($"{nameof(InnerUpdateAsync)}");
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
                    pageEnd.Offset(mainUpdateParameters.PageSize.Value.Width,
                        mainUpdateParameters.PageSize.Value.Height);

                LineInfo2 lineInfo;
                using (var myTextLine = mainUpdateParameters.TextFormatter.FormatLine(customTextSource4,
                    textStorePosition, mainUpdateParameters.ParagraphWidth,
                    genericTextParagraphProperties,
                    null))
                {
                    
                    var nRuns = customTextSource4.Runs.Count - runCount;
#if DEBUGTEXTSOURCE
                    Debug.WriteLine("num runs for line is "  + nRuns);
#endif
                    HandleLine(allCharInfos, linePosition, myTextLine, customTextSource4, runCount, nRuns, lineNo, textStorePosition, runsInfos, mainUpdateParameters.DebugFn);
                    myTextLine.Draw(myDc, linePosition, InvertAxes.None);

                    lineInfo = new LineInfo2(lineNo, allCharInfos.First, textStorePosition, linePosition, myTextLine.Height,
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
                if (lineNo > 0 && lineNo % 100 == 0)
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
                myDc.Close();
                myGroup.Freeze();
                var curUi = new UpdateInfo()
                {
                    DrawingGroup = myGroup, CharInfos = allCharInfos.ToList(),
                    LineInfo = lineInfo
                };
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
#endif
            // await Task.WhenAll(tasks);

            return customTextSource4;
        }

        public static void HandleLine(LinkedList<CharInfo> allCharInfos, Point linePosition, TextLine myTextLine,
            CustomTextSource4 customTextSource4, int runCount, int nRuns, int lineNo, int textStorePosition,
            List<TextRunInfo> runsInfos, Action<string> debugFn=null, TextChange? change = null,
            LineInfo2 curLineInfo = null)
        {
            var curPos = linePosition;
            // var positions = new List<Rect>();
            var indexedGlyphRuns = myTextLine.GetIndexedGlyphRuns();

            var textRuns = customTextSource4.Runs.Skip(runCount).Take(nRuns);

            using (var enum1 = textRuns.GetEnumerator())
            {
                enum1.MoveNext();
                var lineCharIndex = 0;
                var xOrigin = linePosition.X;
                var curCi = allCharInfos.First;
                //var charInfos = new List<CharInfo>(myTextLine.Length);
                if (indexedGlyphRuns != null)
                    foreach (var glyphRunC in indexedGlyphRuns)
                    {
                        var gl = glyphRunC.GlyphRun;
                        var advanceSum = gl.AdvanceWidths.Sum();

                        for (var i = 0; i < gl.Characters.Count; i++)
                        {
                            var i0 = gl.ClusterMap?[i] ?? i;
                            var glAdvanceWidth = gl.AdvanceWidths[i0];
                            var glCharacter = gl.Characters[i];
                            var glCaretStop = gl.CaretStops?[i0];
                            if (curCi != null && curLineInfo != null)
                            {
                                if (lineCharIndex + curLineInfo.Offset >= change.Value.Span.Start)
                                {
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
                        var enum1Current = enum1.Current;
                        var textRunInfo = new TextRunInfo(enum1Current, item);
                        debugFn?.Invoke(textRunInfo.ToString());
                        runsInfos?.Add(textRunInfo);

                        curPos.X += advanceSum;
                        enum1.MoveNext();
                    }
            }
#if DEBUGTEXTSOURCE
                    if (positions.Count != nRuns - 1)
                    {
                        Debug.WriteLine("number of line positions does not match number of runs");
                        var z = string.Join("",
                            indexedGlyphRuns.SelectMany(iz => iz.GlyphRun.Characters));
                        
                        foreach (var textRun in textRuns)
                        {
                            if (textRun is CustomTextCharacters c1)
                            {
                                var tt = c1.Text;
                            } else
                            {

                            }
                        }
                        Debug.WriteLine(z);
                    }
#endif

        }

        public static Point HandleTextLine(ref int textStorePosition, out TextLineBreak prev, ref LineInfo prevLine,
            ref int lineNo, Point linePosition, double paragraphWidth, CustomTextSource4 customTextSource4,
            int runCount,
            TextLine myTextLine, LinkedList<CharInfo> allCharInfos, [NotNull] List<TextRunInfo> runsInfos, DrawingContext myDc,
            out LineInfo lineInfo, bool drawOnSecondaryThread = true, Dispatcher dispatcher = null, JoinableTaskFactory joinableTaskFactory = null)
        {
            Debug.WriteLine("HandleTextLine");
            var nRuns = customTextSource4.Runs.Count - runCount;
#if DEBUGTEXTSOURCE
                    Debug.WriteLine("num runs for line is "  + nRuns);
#endif
            // if (myTextLine.HasOverflowed) Debug.WriteLine("overflowed");

            // if (myTextLine.Width > paragraphWidth) Debug.WriteLine("overflowed2");
            lineInfo = new LineInfo
            {
                Offset = textStorePosition,
                Length = myTextLine.Length,
                PrevLine = prevLine,
                LineNumber = lineNo,
                Height = myTextLine.Height
            };

            if (prevLine != null) prevLine.NextLine = lineInfo;

            prevLine = lineInfo;
            lineInfo.Size = new Size(myTextLine.WidthIncludingTrailingWhitespace, myTextLine.Height);
            lineInfo.Origin = new Point(linePosition.X, linePosition.Y);
            myTextLine.GetTextRunSpans();

            var llNode = allCharInfos.Last;
            var fakeHead = false;
            if (llNode == null)
            {
                fakeHead = true;
                llNode = allCharInfos.AddLast((CharInfo) null);
            }

            var curPos = linePosition;
            var positions = new List<Rect>();
            var indexedGlyphRuns = myTextLine.GetIndexedGlyphRuns();

            var textRuns = customTextSource4.Runs.Skip(runCount).Take(nRuns);
            using (var enum1 = textRuns.GetEnumerator())
            {
                enum1.MoveNext();
                var lineCharIndex = 0;
                var xOrigin = linePosition.X;
                //var charInfos = new List<CharInfo>(myTextLine.Length);
                if (indexedGlyphRuns != null)
                    foreach (var glyphRunC in indexedGlyphRuns)
                    {
                        var gl = glyphRunC.GlyphRun;
                        var advanceSum = gl.AdvanceWidths.Sum();

                        for (var i = 0; i < gl.Characters.Count; i++)
                        {
                            var i0 = gl.ClusterMap?[i] ?? i;
                            var glAdvanceWidth = gl.AdvanceWidths[i0];
                            var glCharacter = gl.Characters[i];
                            var glCaretStop = gl.CaretStops?[i0];
                            var ci = new CharInfo(lineNo, textStorePosition + lineCharIndex, lineCharIndex, i,
                                glCharacter, glAdvanceWidth,
                                glCaretStop, xOrigin, linePosition.Y);
                            lineCharIndex++;
                            xOrigin += glAdvanceWidth;
                            //charInfos.Add(ci);
                            allCharInfos.AddAfter(llNode, ci);
                        }

                        var item = new Rect(curPos, new Size(advanceSum, myTextLine.Height));
                        if (runsInfos != null)
                        {
                            runsInfos.Add(new TextRunInfo(enum1.Current, item));
                            
                        }
                        positions.Add(item);
                        curPos.X += advanceSum;
                        enum1.MoveNext();
                    }
            }
#if DEBUGTEXTSOURCE
                    if (positions.Count != nRuns - 1)
                    {
                        Debug.WriteLine("number of line positions does not match number of runs");
                        var z = string.Join("",
                            indexedGlyphRuns.SelectMany(iz => iz.GlyphRun.Characters));
                        
                        foreach (var textRun in textRuns)
                        {
                            if (textRun is CustomTextCharacters c1)
                            {
                                var tt = c1.Text;
                            } else
                            {

                            }
                        }
                        Debug.WriteLine(z);
                    }
#endif

            //OldHandleTextLine(linePosition, myTextLine, lineInfo, lineChars, cell, cellColumn, prevCell, characterOffset, location, spans, regionOffset);
            var j = new JoinableTaskFactory(new JoinableTaskContext());
            if (fakeHead) allCharInfos.RemoveFirst();
            if (drawOnSecondaryThread)
            {
                myTextLine.Draw(myDc, linePosition, InvertAxes.None);
            }
            else
            {
                // for (var i = 0; i < 3; i++)
                    // CustomTextSource4.DoEvents();
                // ReSharper disable once PossibleNullReferenceException
                // await joinableTaskFactory.SwitchToMainThreadAsync();
                myTextLine.Draw(myDc, linePosition, InvertAxes.None);
                // await j.SwitchToMainThreadAsync();
            }

            linePosition.Y += myTextLine.Height;
            lineNo++;

            prev = null;

            // Update the index position in the text store.
            textStorePosition += myTextLine.Length;
            return linePosition;
        }
       
        private static Typeface CreateTypeface(FontFamily fontFamily, FontStyle fontStyle, FontStretch fontStretch,
            FontWeight fontWeight)
        {
            return new Typeface(fontFamily, fontStyle, fontWeight, fontStretch);
        }

        public static TextFormatter Formatter { get; } = TextFormatter.Create();
    }
}