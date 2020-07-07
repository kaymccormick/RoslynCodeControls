using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.TextFormatting;
using JetBrains.Annotations;
using Microsoft.CodeAnalysis;

namespace RoslynCodeControls
{
    /// <summary>
    /// 
    /// </summary>
    public static class FormattingHelper
    {
        private static TextFormatter Formatter = TextFormatter.Create();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="width"></param>
        /// <param name="currentRendering"></param>
        /// <param name="emSize"></param>
        /// <param name="typeface"></param>
        /// <param name="textDest"></param>
        /// <param name="textStore"></param>
        /// <param name="pixelsPerDip"></param>
        /// <param name="lineInfos"></param>
        /// <param name="infos"></param>
        /// <param name="maxX"></param>
        /// <param name="maxY"></param>
        /// <param name="textLineAction"></param>
        /// <param name="paragraphProperties"></param>
        /// <param name="drawer"></param>
        public static LineContext UpdateFormattedText(double width,
            [NotNull] ref FontRendering currentRendering,
            double emSize,
            [NotNull] Typeface typeface,
            DrawingGroup textDest,
            AppTextSource textStore,
            double pixelsPerDip,
            IList<LineInfo> lineInfos,
            List<RegionInfo> infos,
            ref double maxX,
            out double maxY,
            Action<TextLine, LineContext> textLineAction,
            TextParagraphProperties paragraphProperties, [NotNull] ILineDrawer drawer)
        {
            if (currentRendering == null) throw new ArgumentNullException(nameof(currentRendering));
            if (drawer == null) throw new ArgumentNullException(nameof(drawer));
            Debug.WriteLine(nameof(UpdateFormattedText));
            if (typeface == null)
            {
                Debug.WriteLine($"Null argument typeface");
                throw new ArgumentNullException(nameof(typeface));
            }

            if (currentRendering == null)
            {
                Debug.WriteLine($"Current rendering null, initializing new");
                currentRendering = FontRendering.CreateInstance(emSize,
                    TextAlignment.Left,
                    null,
                    Brushes.Black,
                    typeface);
            }

            var lineContext = new LineContext();

            drawer.PrepareDrawLines(lineContext, true);
            //var dc0 = textDest.Open();
            Debug.WriteLine("Opened drawing group");

            var formatter = Formatter;

            // Format each line of text from the text store and draw it.
            TextLineBreak prev = null;
            var pos = new Point(0, 0);


            Debug.WriteLine($"Text source length is {textStore.Length}");

            while (lineContext.TextStorePosition < textStore.Length)
            {
                lineContext.CurCellRow++;
                using (var myTextLine = formatter.FormatLine(
                    textStore,
                    lineContext.TextStorePosition,
                    width,
                    paragraphProperties,
                    prev))
                {
                    textLineAction?.Invoke(myTextLine, lineContext);
                    lineContext.LineParts.Clear();
                    lineContext.MyTextLine = myTextLine;
                    HandleTextLine(infos, ref lineContext, out var lineInfo, drawer);
                    lineInfos.Add(lineInfo);
                }
            }

            drawer.EndDrawLines(lineContext);

            maxX = lineContext.MaxX;
            maxY = lineContext.MaxY;
            return lineContext;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="width"></param>
        /// <param name="currentRendering"></param>
        /// <param name="emSize"></param>
        /// <param name="typeface"></param>
        /// <param name="textDest"></param>
        /// <param name="textStore"></param>
        /// <param name="pixelsPerDip"></param>
        /// <param name="lineInfos"></param>
        /// <param name="infos"></param>
        /// <param name="maxX"></param>
        /// <param name="maxY"></param>
        /// <param name="textLineAction"></param>
        /// <param name="brush"></param>
        /// <param name="existingRect"></param>
        /// <param name="formatter"></param>
        /// <param name="lineContext"></param>
        /// <param name="paragraphProperties"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static LineContext PartialUpdateFormattedText(double width, ref FontRendering currentRendering,
            double emSize,
            [NotNull] Typeface typeface, DrawingGroup textDest, AppTextSource textStore, double pixelsPerDip,
            IList<LineInfo> lineInfos, List<RegionInfo> infos, ref double maxX, out double maxY,
            Action<TextLine, LineContext> textLineAction, DrawingBrush brush, Rect existingRect,
            TextFormatter formatter,
            LineContext lineContext, TextParagraphProperties paragraphProperties, ILineDrawer lineDrawer)
        {
            if (typeface == null) throw new ArgumentNullException(nameof(typeface));
            if (currentRendering == null)
                currentRendering = FontRendering.CreateInstance(emSize,
                    TextAlignment.Left,
                    null,
                    Brushes.Black,
                    typeface);

            // var dg = new DrawingGroup();
            // var dc1 = dg.Open();
            // dc1.DrawRectangle(brush, null, existingRect);
            // dc1.Close();
            lineDrawer.PrepareDrawLines(lineContext, false);

            var dc0 = textDest.Open();
            // dc0.DrawRectangle(new DrawingBrush(dg), null, existingRect);

            // Format each line of text from the text store and draw it.
            TextLineBreak prev = null;

            while (lineContext.TextStorePosition < textStore.Length)
            {
                lineContext.CurCellRow++;
                using (var myTextLine = formatter.FormatLine(
                    textStore,
                    lineContext.TextStorePosition,
                    width,
                    paragraphProperties,
                    prev))
                {
                    textLineAction?.Invoke(myTextLine, lineContext);
                    lineContext.LineParts.Clear();
                    lineContext.MyTextLine = myTextLine;
                    HandleTextLine(infos, ref lineContext, out var lineInfo, lineDrawer);
                    lineInfos.Add(lineInfo);
                }
            }
            lineDrawer.EndDrawLines(lineContext);

            dc0.Close();
            maxX = lineContext.MaxX;
            maxY = lineContext.MaxY;
            return lineContext;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="infos"></param>
        /// <param name="lineContext"></param>
        /// <param name="lineInfo"></param>
        public static void HandleTextLine(List<RegionInfo> infos, ref LineContext lineContext,
            out LineInfo lineInfo, ILineDrawer drawer)
        {
            lineContext.LineParts.Clear();
            lineContext.TextLineAction?.Invoke(lineContext.MyTextLine);
            //var dd = SaveDrawingGroup(lineContext);

            lineInfo = new LineInfo
            {
                LineNumber = lineContext.LineNumber,
                Offset = lineContext.TextStorePosition,
                Length = lineContext.MyTextLine.Length,
                Size = new Size(lineContext.MyTextLine.WidthIncludingTrailingWhitespace,
                    lineContext.MyTextLine.Height),
                Origin = new Point(lineContext.LineOriginPoint.X, lineContext.LineOriginPoint.Y),
                Height = lineContext.MyTextLine.Height
            };
            Debug.WriteLine($"{lineInfo}");

            var location = lineContext.LineOriginPoint;

            var textRunSpans = lineContext.MyTextLine.GetTextRunSpans();
            var cell = lineContext.LineOriginPoint;
            var cellColumn = 0;
            var characterOffset = lineContext.TextStorePosition;
            var regionOffset = lineContext.TextStorePosition;

            var eol = lineContext.MyTextLine.GetTextRunSpans().Select(xx => xx.Value).OfType<TextEndOfLine>();
            // if (eol.Any())
            // dc.DrawRectangle(Brushes.Aqua, null,
            // new Rect(
            // lineContext.LineOriginPoint.X + lineContext.MyTextLine.WidthIncludingTrailingWhitespace + 2,
            // lineContext.LineOriginPoint.Y + 2, 10, 10));
            Debug.WriteLine("no end of line");
            CharacterCell prevCell = null;
            foreach (var textRunSpan in lineContext.MyTextLine.GetTextRunSpans())
            {
                switch (textRunSpan.Value)
                {
                    case CustomTextCharacters c:
                        lineContext.Offsets.Add(c.Index.GetValueOrDefault());
                        lineContext.LineParts.Add(c.Text);
                        break;
                    case TextEndOfParagraph par:
                        Debug.WriteLine("End of paragraph length is " + par.Length);
                        break;
                    default:
                        Debug.WriteLine("Warning unrhandled element " + textRunSpan.Value.GetType().FullName);
                        break;
                }

                Debug.WriteLine(textRunSpan.Value.ToString());
            }


            var lineRegions = new List<RegionInfo>();

            var group = 0;
            var regionNumber = 0;
            var indexedGlyphRuns = lineContext.MyTextLine.GetIndexedGlyphRuns();
            if (indexedGlyphRuns != null)
                foreach (var rect in indexedGlyphRuns)
                {
                    regionNumber++;
                    var rectGlyphRun = rect.GlyphRun;
                    if (rectGlyphRun == null) continue;
                    var size = new Size(0, 0);
                    var cellBounds =
                        new List<CharacterCell>();
                    var renderingEmSize = rectGlyphRun.FontRenderingEmSize;

                    for (var i = 0; i < rectGlyphRun.Characters.Count; i++)
                    {
                        var advanceWidth = rectGlyphRun.AdvanceWidths[i];
                        size.Width += advanceWidth;
                        var gi = rectGlyphRun.GlyphIndices[i];
                        var c = rectGlyphRun.Characters[i];
                        Debug.WriteLine($"[{characterOffset}] char: " + c);

                        var advWidth = rectGlyphRun.GlyphTypeface.AdvanceWidths[gi];
                        var advHeight = rectGlyphRun.GlyphTypeface.AdvanceHeights[gi];

                        var width = advWidth * renderingEmSize;

                        var height = (advHeight
                                      + rectGlyphRun.GlyphTypeface.BottomSideBearings[gi])
                                     * renderingEmSize;
                        var s = new Size(width,
                            height);

                        var topSide = rectGlyphRun.GlyphTypeface.TopSideBearings[gi];
                        var bounds = new Rect(new Point(cell.X, cell.Y + topSide), s);

                        var charCell = new CharacterCell(bounds, new Point(cellColumn, lineContext.CurCellRow), c);
                        cellBounds.Add(charCell);
                        cell.Offset(advanceWidth, 0);
                        charCell.PreviousCell = prevCell;
                        if (prevCell != null) prevCell.NextCell = charCell;
                        prevCell = charCell;

                        cellColumn++;
                        characterOffset++;
                    }

                    size.Height += lineContext.MyTextLine.Height;
                    var r = new Rect(location, size);
                    location.Offset(size.Width, 0);
                    if (@group < textRunSpans.Count)
                    {
                        var textSpan = textRunSpans[@group];
                        var textSpanValue = textSpan.Value;
                        SyntaxNode node = null;
                        SyntaxToken? token = null;
                        SyntaxTrivia? trivia = null;
                        Debug.WriteLine("text run is length " + textSpanValue.Length);
                        SyntaxToken? AttachedToken = null;
                        SyntaxNode attachedNode = null;
                        switch (textSpanValue)
                        {
                            case SyntaxTokenTextCharacters stc:
                                node = stc.Node;
                                token = stc.Token;
                                break;
                            case SyntaxTriviaTextCharacters stc2:
                                trivia = stc2.Trivia;
                                AttachedToken = stc2.Token;
                                attachedNode = stc2.Node;
                                break;
                        }

                        var tuple = new RegionInfo(textSpanValue, r, cellBounds)
                        {
                            Key = $"{lineContext.LineNumber}.{regionNumber}",
                            Offset = regionOffset,
                            Length = textSpan.Length,
                            SyntaxNode = node,
                            SyntaxToken = token,
                            AttachedToken= AttachedToken,
                            AttachedNode = attachedNode,
                            Trivia = trivia
                        };

                        Debug.WriteLine(tuple.ToString());
                        lineRegions.Add(tuple);
                        infos?.Add(tuple);
                    }

                    @group++;
                    regionOffset = characterOffset;
                }

            lineInfo.Text = string.Join("", lineContext.LineParts);
            Debug.WriteLine("Handled line of text: " + lineInfo.Text);
            lineInfo.Regions = lineRegions;


            Debug.WriteLine($"Drawing text line at origin {lineContext.LineOriginPoint}");
            drawer?.DrawLine(lineContext);
            //lineContext.MyTextLine.Draw(dc, lineContext.LineOriginPoint, InvertAxes.None);

            // ReSharper disable once UnusedVariable
            var p = new Point(lineContext.LineOriginPoint.X + lineContext.MyTextLine.WidthIncludingTrailingWhitespace,
                lineContext.LineOriginPoint.Y);
            Debug.WriteLine("Top right corner of text is " + p);
            var textLineBreak = lineContext.MyTextLine.GetTextLineBreak();
            if (textLineBreak != null) Debug.WriteLine(textLineBreak.ToString());
            lineContext.LineNumber++;

            // Update the index position in the text store.
            Debug.WriteLine("line is length " + lineContext.MyTextLine.Length);
            lineContext.TextStorePosition += lineContext.MyTextLine.Length;
            Debug.WriteLine($"New text store position {lineContext.TextStorePosition}");
            // Update the line position coordinate for the displayed line.
            lineContext.LineOriginPoint = new Point(lineContext.LineOriginPoint.X,
                lineContext.LineOriginPoint.Y + lineContext.MyTextLine.Height);
            if (lineContext.LineOriginPoint.Y >= lineContext.MaxY) lineContext.MaxY = lineContext.LineOriginPoint.Y;
            if (lineContext.MyTextLine.Width >= lineContext.MaxX) lineContext.MaxX = lineContext.MyTextLine.Width;
            var lastLine = lineContext.LineInfo;
            if (lastLine != null)
            {
                lastLine.NextLine = lineInfo;
                lineInfo.PrevLine = lastLine;
            }

            lineContext.LineInfo = lineInfo;
        }

        private static DrawingGroup SaveDrawingGroup(LineContext lineContext)
        {
            var dd = new DrawingGroup();
            var dc1 = dd.Open();
            dc1.DrawRectangle(Brushes.White, null,
                new Rect(0, 0, lineContext.MyTextLine.WidthIncludingTrailingWhitespace, lineContext.MyTextLine.Height));
            lineContext.MyTextLine.Draw(dc1, new Point(0, 0), InvertAxes.None);
            dc1.Close();
            var imgWidth = (int) dd.Bounds.Width;
            var imgHeight = (int) dd.Bounds.Height;
            if (imgWidth > 0 && imgHeight > 0)
                SaveImage(dd, lineContext.LineNumber.ToString(),
                    imgWidth, imgHeight);
            return dd;
        }

        private static void SaveImage(DrawingGroup drawingGroup,
            string filePrefix, int width, int height)
        {
            Debug.WriteLine("Creating image " + $"({width},{height}) {filePrefix}.png");
            var v = new DrawingVisual();
            var dc = v.RenderOpen();
            var bounds = drawingGroup.Bounds;

            var brush = new DrawingBrush(drawingGroup);
            dc.DrawRectangle(
                brush, null, bounds);
            dc.Close();
            var rtb = new RenderTargetBitmap(width, height, 96,
                96,
                PixelFormats.Pbgra32);
            rtb.Render(v);

            var png = new PngBitmapEncoder();
            png.Frames.Add(BitmapFrame.Create(rtb));
            var fname = $"{filePrefix}.png";
            using (var s = File.Create("C:\\temp\\" + fname))
            {
                png.Save(s);
            }
        }

    }
}