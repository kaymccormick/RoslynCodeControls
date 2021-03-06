﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.TextFormatting;
using JetBrains.Annotations;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using CSharpExtensions = Microsoft.CodeAnalysis.CSharp.CSharpExtensions;
using SyntaxFacts = Microsoft.CodeAnalysis.CSharp.SyntaxFacts;
using SyntaxKind = Microsoft.CodeAnalysis.CSharp.SyntaxKind;
using TextChange = Microsoft.CodeAnalysis.Text.TextChange;
// ReSharper disable InvokeAsExtensionMethod
#pragma warning disable 8629

#pragma warning disable 8602
#pragma warning disable 8618

// ReSharper disable ConvertSwitchStatementToSwitchExpression
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable UnusedAutoPropertyAccessor.Local
#pragma warning disable 162

namespace RoslynCodeControls
{
    /// <summary>
    /// 
    /// </summary>
    public sealed class CustomTextSource4 : AppTextSource, ICustomTextSource, INotifyPropertyChanged
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="pixelsPerDip"></param>
        /// <param name="fontRendering"></param>
        /// <param name="genericTextRunProperties"></param>
        /// <param name="debugFn"></param>
        public CustomTextSource4(double pixelsPerDip, FontRendering fontRendering,
            GenericTextRunProperties genericTextRunProperties, RoslynCodeBase.DebugDelegate debugFn)
        {
            CurrentRendering = fontRendering;
            PixelsPerDip = pixelsPerDip;

            BaseProps = genericTextRunProperties;
            _debugFn = debugFn;
        }


        /// <summary>
        /// 
        /// </summary>
        public Compilation Compilation { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public override int Length { get; protected set; }


        private IEnumerable<SyntaxInfo> GetSyntaxInfos(int lineInfoOffset = 0)
        {
#if DEBUG
            _debugFn?.Invoke($"{nameof(GetSyntaxInfos)} [{lineInfoOffset}]", 5);
#endif
            SyntaxToken token1;
            if (lineInfoOffset != 0)
            {
                if (lineInfoOffset < Length)
                {
                    var child = Node.ChildThatContainsPosition(lineInfoOffset);
                    if (child.SpanStart > lineInfoOffset)
                        if (child.HasLeadingTrivia)
                            foreach (var syntaxTrivia in child.GetLeadingTrivia())
                                yield return new SyntaxInfo(syntaxTrivia, Node);

                    // ReSharper disable once PossibleNullReferenceException
                    token1 = child.IsToken ? child.AsToken() : child.AsNode().GetFirstToken();
                }
                else
                {
                    yield break;
                }
            }
            else
            {
                if (Node == null)
                    yield break;
                if (Node.HasLeadingTrivia)
                    foreach (var syntaxTrivia in Node.GetLeadingTrivia())
                        yield return new SyntaxInfo(syntaxTrivia, Node);
                token1 = Node.GetFirstToken();
            }


            // ReSharper disable once InvokeAsExtensionMethod
            while (CSharpExtensions.Kind(token1) != SyntaxKind.None)
            {
                // ReSharper disable once InvokeAsExtensionMethod
                if (CSharpExtensions.Kind(token1) == SyntaxKind.EndOfFileToken)
                    yield break;
                yield return new SyntaxInfo(token1);
                if (token1.HasTrailingTrivia)
                    foreach (var syntaxTrivia in token1.TrailingTrivia)
                        // ReSharper disable once ConditionIsAlwaysTrueOrFalse
                        if (false && syntaxTrivia.IsPartOfStructuredTrivia())
                            // ReSharper disable once HeuristicUnreachableCode
                        {
                            var syntaxNode = syntaxTrivia.GetStructure();
                            var n = syntaxTrivia.Token.Parent;
                            if (n is StructuredTriviaSyntax sn)
                            {
                                var trail = sn.GetTrailingTrivia();
                            }

                            // while (syntaxNode == null)
                            // {
                            // syntaxNode = n.ParentTrivia;
                            // }
                            // syntaxNode = syntaxTrivia.GetStructure();
                            var token2 = syntaxNode.GetFirstToken(true, true, true, true);
                            while (CSharpExtensions.Kind(token2) != SyntaxKind.None)
                            {
                                if (token2.HasLeadingTrivia)
                                    foreach (var syntaxTrivia2 in token2.LeadingTrivia)
                                        yield return new SyntaxInfo(syntaxTrivia2, token2, TriviaPosition.Leading)
                                            {StructuredTrivia = syntaxNode};

                                yield return new SyntaxInfo(token2) {StructuredTrivia = syntaxNode};
                                if (token2.HasTrailingTrivia)
                                    foreach (var syntaxTrivia2 in token2.TrailingTrivia)
                                        yield return new SyntaxInfo(syntaxTrivia2, token2, TriviaPosition.Trailing)
                                            {StructuredTrivia = syntaxNode};

                                token2 = token2.GetNextToken(true, true, true, true);
                            }
                        }
                        else
                        {
                            yield return new SyntaxInfo(syntaxTrivia, token1, TriviaPosition.Trailing);
                        }

                token1 = token1.GetNextToken(true, true, true, true);
                if (token1.HasLeadingTrivia)
                    foreach (var syntaxTrivia in token1.LeadingTrivia)
                        // if (syntaxTrivia.IsPartOfStructuredTrivia())
                        // {

                        // var token2 = syntaxTrivia.GetStructure().GetFirstToken(true, true, true, true);
                        // while (CSharpExtensions.Kind(token2) != SyntaxKind.None)
                        // {
                        // if (token2.HasLeadingTrivia)
                        // {
                        // foreach (var syntaxTrivia2 in token2.LeadingTrivia)
                        // {
                        // yield return new SyntaxInfo(syntaxTrivia2);
                        // }
                        // }

                        // yield return new SyntaxInfo(token2);
                        // if (token2.HasTrailingTrivia)
                        // {
                        // foreach (var syntaxTrivia2 in token2.TrailingTrivia)
                        // {
                        // yield return new SyntaxInfo(syntaxTrivia2);
                        // }
                        // }

                        // token2 = token2.GetNextToken(true, true, true, true);
                        // }
                        // }
                        // else
                        // {
                        yield return new SyntaxInfo(syntaxTrivia, token1, TriviaPosition.Leading);
                // }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="textSourceCharacterIndex"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public override TextRun GetTextRun(int textSourceCharacterIndex)
        {
            var result = _GetTextRun(textSourceCharacterIndex);
            _debugFn?.Invoke("Got " + result, 5);
            return result;
        }

        private TextRun _GetTextRun(int textSourceCharacterIndex)
        {
            _charIndex = textSourceCharacterIndex;
            _debugFn?.Invoke($"GetTextRun(textSourceCharacterIndex = {textSourceCharacterIndex})", 5);

            if (textSourceCharacterIndex == 0)
            {
#if DEBUG
                _debugFn?.Invoke($"Clearing runs because at beginning of text source", 4);
#endif
                Runs.Clear();
                SyntaxInfos = GetSyntaxInfos().GetEnumerator();
                if (!SyntaxInfos.MoveNext())
                {
                    var endOfParagraph = new CustomTextEndOfParagraph(2) {Index = textSourceCharacterIndex};
                    Runs.Add(endOfParagraph);
                    return endOfParagraph;
                }
            }
            else
            {
#if DEBUGRUNS
                foreach (var textRun in Runs) _debugFn?.Invoke("    " + textRun.ToString(), 4);
#endif

                Runs = RunsBefore(textSourceCharacterIndex, Runs).ToList();
            }

            var si = SyntaxInfos.Current;
            if (si == null)
            {
                var endOfParagraph = new CustomTextEndOfParagraph(2) {Index = textSourceCharacterIndex};
                Runs.Add(endOfParagraph);
                return endOfParagraph;
            }

            // ReSharper disable once PossibleNullReferenceException
            while (textSourceCharacterIndex > si.Span1.Start
                /*|| si.Span1.End < textSourceCharacterIndex*/ || si.Text.Length == 0)
            {
                if (!SyntaxInfos.MoveNext())
                {
                    if (textSourceCharacterIndex < Length)
                    {
                        var len = Length - textSourceCharacterIndex;
                        var buf = new char[len];
                        Text.CopyTo(textSourceCharacterIndex, buf, 0, len);
                        if (len == 2 && buf[0] == '\r' && buf[1] == '\n')
                        {
                            var eol = new CustomTextEndOfLine(2) {Index = textSourceCharacterIndex};
                            Runs.Add(eol);
                            return eol;
                        }

                        var t = string.Join("", buf);
                        var customTextCharacters = new CustomTextCharacters(t, MakeProperties(SyntaxKind.None, t))
                            {Index = textSourceCharacterIndex};
                        Runs.Add(customTextCharacters);
                        return customTextCharacters;
                    }

                    var endOfParagraph = new CustomTextEndOfParagraph(2) {Index = textSourceCharacterIndex};
                    Runs.Add(endOfParagraph);
                    return endOfParagraph;
                }

                si = SyntaxInfos.Current;
            }

            if (textSourceCharacterIndex < si.Span1.Start)
            {
                var len = si.Span1.Start - textSourceCharacterIndex;
                var buf = new char[len];
                Text.CopyTo(textSourceCharacterIndex, buf, 0, len);
                if (len == 2 && buf[0] == '\r' && buf[1] == '\n')
                {
                    var eol = new CustomTextEndOfLine(2) {Index = textSourceCharacterIndex};

                    Runs.Add(eol);
                    return eol;
                }

                var t = string.Join("", buf);
                var nl = t.IndexOf("\r\n", StringComparison.Ordinal);
                if (nl != -1)
                {
                    t = t.Substring(0, nl);
                    if (t == "")
                    {
                        var eol = new CustomTextEndOfLine(2) {Index = textSourceCharacterIndex};
                        Runs.Add(eol);
                        return eol;
                    }

                    var ctc = new CustomTextCharacters(t,
                            MakeProperties(SyntaxKind.None, t))
                        {Index = textSourceCharacterIndex};
                    Runs.Add(ctc);
                    return ctc;
                }

                var customTextCharacters = new CustomTextCharacters(t, MakeProperties(SyntaxKind.None, t))
                    {Index = textSourceCharacterIndex};
                Runs.Add(customTextCharacters);
                return customTextCharacters;
            }

            // while (textSourceCharacterIndex > si.Span1.Start)
            // {
            // if (!SyntaxInfos.MoveNext())
            // {

            // }
            // }
            if (textSourceCharacterIndex != si.Span1.Start)
                throw new InvalidOperationException("Character index does not match span start");
            if (si.SyntaxTrivia.HasValue)
            {
                var syntaxKind = CSharpExtensions.Kind(si.SyntaxTrivia.Value);
                if (syntaxKind == SyntaxKind.EndOfLineTrivia || syntaxKind == SyntaxKind.XmlTextLiteralNewLineToken)
                {
                    var customTextEndOfLine = new CustomTextEndOfLine(2) {Index = textSourceCharacterIndex};
                    Runs.Add(customTextEndOfLine);
                    return customTextEndOfLine;
                }

                var p = PropsFor(si.SyntaxTrivia.Value, si.Text);
                var syntaxTriviaTextCharacters = new SyntaxTriviaTextCharacters(si.Text, p, si.Span1,
                        si.SyntaxTrivia.Value, si.Node, si.Token, si.TriviaPosition, si.StructuredTrivia)
                    {Index = si.Span1.Start};
                Runs.Add(syntaxTriviaTextCharacters);
                return syntaxTriviaTextCharacters;
            }

            if (si.SyntaxToken.HasValue)
            {
                if (CSharpExtensions.Kind(si.SyntaxToken.Value) == SyntaxKind.XmlTextLiteralNewLineToken)
                {
                    var customTextEndOfLine = new CustomTextEndOfLine(2) {Index = textSourceCharacterIndex};
                    Runs.Add(customTextEndOfLine);
                    return customTextEndOfLine;
                }

                var syntaxTokenTextCharacters = new SyntaxTokenTextCharacters(si.Text, si.Text.Length,
                    PropsFor(si.SyntaxToken.Value, si.Text),
                    si.SyntaxToken.Value, si.SyntaxToken.Value.Parent) {Index = si.Span1.Start};
                Runs.Add(syntaxTokenTextCharacters);
                return syntaxTokenTextCharacters;
            }

            var textEndOfParagraph = new CustomTextEndOfParagraph(2) {Index = textSourceCharacterIndex};
            Runs.Add(textEndOfParagraph);
            return textEndOfParagraph;

#if false
            Debug.WriteLine($"index: {textSourceCharacterIndex}");

            TextSpan? TakeToken()
            {
                var includeDocumentationComments = true;
                var includeDirectives = true;
                var includeSkipped = true;
                var includeZeroWidth = true;
                token = token.HasValue
                    ? token.Value.GetNextToken(includeZeroWidth, includeSkipped, includeDirectives,
                        includeDocumentationComments)
                    : Node?.GetFirstToken(includeZeroWidth, includeSkipped, includeDirectives,
                        includeDocumentationComments);

                if (token.HasValue)
                {
                    if (!_starts.Any() && token.Value.SpanStart != 0)
                    {
                    }

                    var tuple = new StartInfo(token.Value.Span, token.Value);
                    _starts.Add(tuple);
                    DumpStarts();
                    return token.Value.Span;
                }

                return null;
            }

            TextSpan? span = null;
            if (textSourceCharacterIndex == 0)
            {
                if (Length == 0) return new TextEndOfParagraph(2);

                _curStart = 0;

                if (_starts.Any())
                {
                    var startInfo = _starts.First();
                    token = startInfo.Token;
                    trivia = startInfo.SyntaxTrivia;
                    span = startInfo.TextSpan;
                    if (token.HasValue) CheckToken(token);
                }

                // _starts.Clear();
                DumpStarts();
            }
            else
            {
                var startInfo = _starts[_curStart];
                token = startInfo.Token;
                trivia = startInfo.SyntaxTrivia;
                span = startInfo.TextSpan;
                if (token.HasValue) CheckToken(token);
            }

            try
            {
                var childInPos = Node.ChildThatContainsPosition(textSourceCharacterIndex);
                if (childInPos.IsNode)
                {
                    var n = childInPos.AsNode();
                    if (textSourceCharacterIndex < n.SpanStart)
                        foreach (var syntaxTrivia in n.GetLeadingTrivia())
                            if (textSourceCharacterIndex >= syntaxTrivia.SpanStart &&
                                textSourceCharacterIndex < syntaxTrivia.Span.End)
                            {
                                Debug.WriteLine("In trivia " + syntaxTrivia);
                                if (textSourceCharacterIndex > syntaxTrivia.SpanStart)
                                    Debug.WriteLine("In middle of trivia");

                                var characterString = syntaxTrivia.ToFullString();
                                return new SyntaxTriviaTextCharacters(characterString,
                                    PropsFor(syntaxTrivia, characterString), syntaxTrivia.FullSpan, syntaxTrivia, null,
                                    null, TriviaPosition.Leading);
                            }
                }
            }
            catch (Exception ex)
            {
            }


            var token1 = token;
            // Debug.WriteLine("Index = " + textSourceCharacterIndex);
            // if (!token1.HasValue)
            // {
            // span = TakeToken();
            // if (!this.token.HasValue) return new TextEndOfParagraph(2);
            // token1 = this.token;

            // }

//            var token = token1.Value;
            if (!span.HasValue) throw new InvalidOperationException();
            var k = span.Value;

            if (textSourceCharacterIndex < k.Start)
            {
                var len = k.Start - textSourceCharacterIndex;
                var buf = new char[len];
                Text.CopyTo(textSourceCharacterIndex, buf, 0, len);
                if (len == 2 && buf[0] == '\r' && buf[1] == '\n') return new CustomTextEndOfLine(2);

                var t = string.Join("", buf);
                return new CustomTextCharacters(t, MakeProperties(SyntaxKind.None, t));
            }
            else if (textSourceCharacterIndex >= k.End && k.Length != 0)
            {
                TakeToken();
                return GetTextRun(textSourceCharacterIndex);
            }
            else
            {
                if (trivia.HasValue)
                {
                    var syntaxTrivia1 = trivia.Value;
                    var q = syntaxTrivia1.Token.LeadingTrivia
                        .SkipWhile(syntaxTrivia => syntaxTrivia != syntaxTrivia1)
                        .Skip(1);
                    if (q.Any())
                    {
                        _curStart++;
                        var startInfo = new StartInfo(q.First());
                        if (_starts.Count <= _curStart)
                            _starts.Add(startInfo);
                        else
                            _starts[_curStart] = startInfo;
                    }
                    else
                    {
                        var t2 = syntaxTrivia1.Token.GetNextToken(true, true, true, true);
                        if (t2.HasLeadingTrivia)
                        {
                            var st = new StartInfo(t2.LeadingTrivia.First());
                            _curStart++;
                            if (_starts.Count <= _curStart)
                                _starts.Add(st);
                            else
                                _starts[_curStart] = st;
                        }
                        else if (CSharpExtensions.Kind(t2) != SyntaxKind.None)
                        {
                            var st = new StartInfo(t2.Span, t2);
                            _curStart++;
                            if (_starts.Count <= _curStart)
                                _starts.Add(st);
                            else
                                _starts[_curStart] = st;
                        }
                    }

                    var t = syntaxTrivia1.ToFullString();
                    return new SyntaxTriviaTextCharacters(t, PropsFor(trivia.Value, t), span.Value, syntaxTrivia1, null,
                        null, TriviaPosition.Leading);
                }

                if (token.HasValue && (CSharpExtensions.Kind(token.Value) == SyntaxKind.None ||
                                       CSharpExtensions.Kind(token.Value) == SyntaxKind.EndOfFileToken))
                    return new TextEndOfParagraph(2);
                var token0 = token.Value;
                if (CSharpExtensions.Kind(token0) == SyntaxKind.EndOfLineTrivia) return new CustomTextEndOfLine(2);
                var len = k.Length;
                if (len == 0)
                {
                    TakeToken();
                    return GetTextRun(textSourceCharacterIndex);
                }

                TakeToken();
                if (token0.Text.Length != len)
                {
                }

                return new CustomTextCharacters(token0.Text, MakeProperties(token, token0.Text));
            }
#endif
        }

        public static IEnumerable<TextRun> RunsBefore(int textSourceCharacterIndex, IEnumerable<TextRun> textRuns)
        {
            return textRuns.Where(r =>
            {
                switch (r)
                {
                    case CustomTextEndOfLine customTextEndOfLine:
                        // ReSharper disable once PossibleInvalidOperationException
                        return customTextEndOfLine.Index.Value + customTextEndOfLine.Length <=
                               textSourceCharacterIndex;
                    case CustomTextEndOfParagraph customTextEndOfParagraph:
                        // ReSharper disable once PossibleInvalidOperationException
                        return customTextEndOfParagraph.Index.Value + customTextEndOfParagraph.Length <=
                               textSourceCharacterIndex;

                    case SyntaxTokenTextCharacters syntaxTokenTextCharacters:
                        // ReSharper disable once PossibleInvalidOperationException
                        return syntaxTokenTextCharacters.Index.Value + syntaxTokenTextCharacters.Length <=
                               textSourceCharacterIndex;

                    case SyntaxTriviaTextCharacters syntaxTriviaTextCharacters:
                        // ReSharper disable once PossibleInvalidOperationException
                        return syntaxTriviaTextCharacters.Index.Value + syntaxTriviaTextCharacters.Length <=
                               textSourceCharacterIndex;
                    case CustomTextCharacters customTextCharacters:
                        // ReSharper disable once PossibleInvalidOperationException
                        return customTextCharacters.Index.Value + customTextCharacters.Length <
                               textSourceCharacterIndex;
                    default:
                        throw new InvalidOperationException();
                }
            });
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="textSourceCharacterIndexLimit"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public override TextSpan<CultureSpecificCharacterBufferRange> GetPrecedingText(
            int textSourceCharacterIndexLimit)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="textSourceCharacterIndex"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public override int GetTextEffectCharacterIndexFromTextSourceCharacterIndex(int textSourceCharacterIndex)
        {
            throw new Exception("The method or operation is not implemented.");
        }


        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public override TextRunProperties BasicProps()
        {
            return new VeryBasicTextRunProperties(BaseProps);
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="trivia"></param>
        /// <param name="text"></param>
        /// <returns></returns>
        // ReSharper disable once UnusedParameter.Local
        private TextRunProperties PropsFor(in SyntaxTrivia trivia, string text)
        {
            // var r = BasicProps();
            // ReSharper disable once InvokeAsExtensionMethod
            var syntaxKind = CSharpExtensions.Kind(trivia);
#if DEBUGTEXTSOURCE
            Debug.WriteLine($"{syntaxKind}", DebugCategory.TextFormatting);
#endif
            if (syntaxKind == SyntaxKind.SingleLineCommentTrivia || syntaxKind == SyntaxKind.MultiLineCommentTrivia)
                return new GenericTextRunProperties(CurrentRendering, PixelsPerDip, Brushes.YellowGreen);


            return new GenericTextRunProperties(CurrentRendering, PixelsPerDip, Brushes.YellowGreen);
        }


        /// <summary>
        /// 
        /// </summary>
        public SyntaxNode Node { get; set; }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="arg"></param>
        /// <param name="text"></param>
        /// <returns></returns>
        public TextRunProperties MakeProperties(object arg, string text)
        {
            TextRunProperties textRunProperties;
            if (arg == (object) SyntaxKind.None)
                textRunProperties = PropsFor(text);
            else
                switch (arg)
                {
                    case SyntaxTrivia st:
                        textRunProperties = PropsFor(st, text);
                        break;
                    case SyntaxToken t:
                        textRunProperties = PropsFor(t, text);
                        break;
                    default:
                        textRunProperties = PropsFor(text);
                        break;
                }
#if DEBUG
            if (textRunProperties is BasicTextRunProperties b)
                if (!b.HasCustomization)
                {
                    // b.SetBackgroundBrush(Brushes.LightBlue);
                    var kind = "";

                    var nodeKind = "";
                    var tkind = "";
                    if (!(arg is SyntaxToken stk)) return textRunProperties;
                    var syntaxKind = CSharpExtensions.Kind(stk.Parent);
                    nodeKind = syntaxKind.ToString();
                    kind = CSharpExtensions.Kind(stk).ToString();
                    if (SyntaxFacts.IsTrivia(syntaxKind))
                    {
                        var pt = stk.Parent.ParentTrivia;
                        tkind = CSharpExtensions.Kind(pt).ToString();
                    }

                    // Debug.WriteLine(
                    // $"no customizations for {arg} - {text} {arg.GetType().Name} {kind} {nodeKind} [{tkind}]");
                }


#endif

            return textRunProperties;
        }

        /// <summary>
        /// 
        /// </summary>
        public SyntaxTree Tree
        {
            get { return _tree; }
            set
            {
                _tree = value;
                if (_tree != null)
                {
                    _debugFn?.Invoke("Tree set, resetting enumerator");
                    Text = _tree.GetText();
                    SyntaxInfos = GetSyntaxInfos().GetEnumerator();
                    SyntaxInfos.MoveNext();
                    Length = Text.Length;
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="token"></param>
        /// <param name="text"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        // ReSharper disable once UnusedParameter.Local
        private TextRunProperties PropsFor(SyntaxToken token, string text)
        {
            // var pp = BasicProps();
            var kind = CSharpExtensions.Kind(token);
            switch (kind)
            {
                case SyntaxKind.OpenBraceToken:
                case SyntaxKind.CloseBraceToken:
                case SyntaxKind.OpenBracketToken:
                case SyntaxKind.CloseBracketToken:
                case SyntaxKind.OpenParenToken:
                case SyntaxKind.CloseParenToken:
                case SyntaxKind.LessThanToken:
                case SyntaxKind.GreaterThanToken:
                    return new GenericTextRunProperties(CurrentRendering, PixelsPerDip, Brushes.Red);
                // pp.SetForegroundBrush(Brushes.Red);
            }

            if (token.Parent == null) return new GenericTextRunProperties(CurrentRendering, PixelsPerDip);

            var syntaxKind = CSharpExtensions.Kind(token.Parent);
            var zz = token.Parent.FirstAncestorOrSelf<SyntaxNode>(
                z => SyntaxFacts.IsTrivia(CSharpExtensions.Kind(z)), false);

            if (zz != null)
            {
                var pt = zz.ParentTrivia;
                var syntaxKind1 = CSharpExtensions.Kind(pt);


                // ReSharper disable once SwitchStatementMissingSomeEnumCasesNoDefault
                switch (syntaxKind1)
                {
                    case SyntaxKind.EndOfLineTrivia:
                        break;
                    case SyntaxKind.WhitespaceTrivia:
                        break;
                    case SyntaxKind.SingleLineCommentTrivia:
                    case SyntaxKind.MultiLineCommentTrivia:
                    case SyntaxKind.SingleLineDocumentationCommentTrivia:
                    case SyntaxKind.MultiLineDocumentationCommentTrivia:
                    case SyntaxKind.DisabledTextTrivia:

                        return new GenericTextRunProperties(CurrentRendering, PixelsPerDip, Brushes.SlateGray);
                    case SyntaxKind.DocumentationCommentExteriorTrivia:
                        return new GenericTextRunProperties(CurrentRendering, PixelsPerDip, Brushes.Aqua);
                    case SyntaxKind.PreprocessingMessageTrivia:
                        break;
                    case SyntaxKind.IfDirectiveTrivia:

                    case SyntaxKind.ElifDirectiveTrivia:
                    case SyntaxKind.ElseDirectiveTrivia:
                    case SyntaxKind.EndIfDirectiveTrivia:
                    case SyntaxKind.RegionDirectiveTrivia:
                    case SyntaxKind.EndRegionDirectiveTrivia:
                    case SyntaxKind.DefineDirectiveTrivia:
                    case SyntaxKind.UndefDirectiveTrivia:
                    case SyntaxKind.ErrorDirectiveTrivia:
                    case SyntaxKind.WarningDirectiveTrivia:
                    case SyntaxKind.LineDirectiveTrivia:
                        return new GenericTextRunProperties(CurrentRendering, PixelsPerDip, Brushes.BurlyWood);
                    case SyntaxKind.PragmaWarningDirectiveTrivia:
                        break;
                    case SyntaxKind.PragmaChecksumDirectiveTrivia:
                        break;
                    case SyntaxKind.ReferenceDirectiveTrivia:
                        break;
                    case SyntaxKind.BadDirectiveTrivia:
                        break;
                    case SyntaxKind.SkippedTokensTrivia:
                        break;
                    case SyntaxKind.ConflictMarkerTrivia:
                        break;
                    case SyntaxKind.NullableDirectiveTrivia:
                        break;
                }
            }

            if (SyntaxFacts.IsPredefinedType(kind))
                return new GenericTextRunProperties(CurrentRendering, PixelsPerDip, Brushes.Gold);
            // pp.SetForegroundBrush(Brushes.Gold);
            else if (SyntaxFacts.IsKeywordKind(kind))
                return new GenericTextRunProperties(CurrentRendering, PixelsPerDip, Brushes.CornflowerBlue);
            // pp.SetForegroundBrush(Brushes.CornflowerBlue);
            // return pp;
            else if (SyntaxFacts.IsLiteralExpression(kind))
                return new GenericTextRunProperties(CurrentRendering, PixelsPerDip, Brushes.Brown,
                    FontStyles.Italic);

#if DEBUGTEXTSOURCE
Debug.WriteLine(syntaxKind.ToString(), DebugCategory.TextFormatting);
#endif
            // if (SyntaxFacts.IsName(syntaxKind))
            // pp.SetForegroundBrush(Brushes.Pink);
            // if (SyntaxFacts.IsTypeSyntax(syntaxKind)) pp.SetForegroundBrush(Brushes.Crimson);

            if (syntaxKind == SyntaxKind.MethodDeclaration)
            {
                if (SyntaxFacts.IsAccessibilityModifier(kind))
                    return new GenericTextRunProperties(CurrentRendering, PixelsPerDip, Brushes.Aqua);

                else if (SyntaxFacts.IsKeywordKind(kind))
                    return new GenericTextRunProperties(CurrentRendering, PixelsPerDip, null, FontStyles.Italic);
            }

            // pp.SyntaxToken = trivia;
            // pp.Text = text;

            return new GenericTextRunProperties(CurrentRendering, PixelsPerDip);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        // ReSharper disable once UnusedParameter.Local
        private TextRunProperties PropsFor(string text)
        {
            // var pp = BasicProps();

            if (text.Trim().Length == 0) return new GenericTextRunProperties(CurrentRendering, PixelsPerDip);

            return new GenericTextRunProperties(CurrentRendering, PixelsPerDip, Brushes.Fuchsia);

            // pp.SetBackgroundBrush(Brushes.Black);
            // return pp;
        }

        public double EmSize { get; set; } = 24;

        public override GenericTextRunProperties BaseProps { get; set; }

        private SyntaxTree _tree;

        private IEnumerator<SyntaxInfo> _syntaxInfos;
        private readonly RoslynCodeBase.DebugDelegate _debugFn;

        private int _charIndex;

        // private ObservableCollection<TextRun> _runs = new ObservableCollection<TextRun>();
        public int EolLength { get; } = 2;


        /// <summary>
        /// 
        /// </summary>
        public override void Init()
        {
        }

        public async Task<object> TextInputAsync(int insertionPoint, InputRequest inputRequest, int lineInfoOffset)
        {
            var text = inputRequest.Text;
#if DEBUG
            _debugFn?.Invoke($"Insertion point is {insertionPoint}.", 3);
            _debugFn?.Invoke($"Input text is \"{text}\"", 3);
#endif
            TextChange change = inputRequest.Kind == InputRequestKind.Backspace ?
                Text[insertionPoint - 1] == '\n' && Text[insertionPoint - 2] == '\r'
                ? new TextChange(new TextSpan(insertionPoint - 2, 2), "")
                    : new TextChange(new TextSpan(insertionPoint - 1, 1), "")
                    : new TextChange(new TextSpan(insertionPoint, 0), text);

            var newText = Text.WithChanges(change);
            if (text != null && newText.Length != Text.Length + text.Length) Debug.WriteLine($"Unexpected length");
            var newTree = Tree.WithChangedText(newText);

            var syntaxNode = await newTree.GetRootAsync().ConfigureAwait(true);
            _tree = newTree;
            Length = newText.Length;
            Node = syntaxNode;
            Text = newText;
            SyntaxInfos = GetSyntaxInfos(lineInfoOffset).GetEnumerator();
            SyntaxInfos.MoveNext();


            return change;
        }

        private IEnumerator<SyntaxInfo> SyntaxInfos
        {
            get { return _syntaxInfos; }
            set
            {
                _syntaxInfos = value;
#if DEBUGTEXTSOURCE
                Debug.WriteLine("Syntax enumerator set");
#endif
            }
        }

        public SourceText Text { get; set; }


        public List<TextRunInfo> RunInfos { get; set; }

        public List<TextRun> Runs { get; set; } = new List<TextRun>();

        public FontRendering CurrentRendering { get; set; }


        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void SetText(string code)
        {
            var tree = SyntaxFactory.ParseSyntaxTree(code);
            Node = tree.GetRoot();
            Tree = tree;
        }

        public static IEnumerable<TextRunInfo> RunInfosBefore(in int offset, List<TextRunInfo> sourceRuns)
        {
            return sourceRuns.Take(RunsBefore(offset, sourceRuns.Select(zz => zz.TextRun)).Count()).ToList();
        }
    }
}