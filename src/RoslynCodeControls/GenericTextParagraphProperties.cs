using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Media.TextFormatting;

namespace RoslynCodeControls
{
    /// <summary>
    /// Class to implement TextParagraphProperties, used by TextSource
    /// </summary>
    public class GenericTextParagraphProperties : TextParagraphProperties
    {
        #region Constructors

        public GenericTextParagraphProperties(
            FlowDirection flowDirection,
            TextAlignment textAlignment,
            bool firstLineInParagraph,
            bool alwaysCollapsible,
            TextRunProperties defaultTextRunProperties,
            TextWrapping textWrap,
            double lineHeight,
            double indent)
        {
            _flowDirection = flowDirection;
            _textAlignment = textAlignment;
            _firstLineInParagraph = firstLineInParagraph;
            _alwaysCollapsible = alwaysCollapsible;
            _defaultTextRunProperties = defaultTextRunProperties;
            _textWrap = textWrap;
            _lineHeight = lineHeight;
            _indent = indent;
        }

        public GenericTextParagraphProperties(FontRendering newRendering, double pixelsPerDip)
        {
            _flowDirection = FlowDirection.LeftToRight;
            _textAlignment = newRendering.TextAlignment;
            _firstLineInParagraph = false;
            _alwaysCollapsible = false;
            _defaultTextRunProperties = new GenericTextRunProperties(
                newRendering.Typeface, pixelsPerDip, newRendering.FontSize, newRendering.FontSize,
                newRendering.TextDecorations, newRendering.TextColor, null,
                BaselineAlignment.Baseline, CultureInfo.CurrentUICulture);
            _textWrap = TextWrapping.Wrap;
            _lineHeight = 0;
            _indent = 0;
            _paragraphIndent = 0;
        }

        #endregion

        #region Properties

        public override FlowDirection FlowDirection
        {
            get { return _flowDirection; }
        }

        public override TextAlignment TextAlignment
        {
            get { return _textAlignment; }
        }

        public override bool FirstLineInParagraph
        {
            get { return _firstLineInParagraph; }
        }

        public override bool AlwaysCollapsible
        {
            get { return _alwaysCollapsible; }
        }

        public override TextRunProperties DefaultTextRunProperties
        {
            get { return _defaultTextRunProperties; }
        }

        public override TextWrapping TextWrapping
        {
            get { return _textWrap; }
        }

        public override double LineHeight
        {
            get { return _lineHeight; }
        }

        public override double Indent
        {
            get { return _indent; }
        }

        public override TextMarkerProperties TextMarkerProperties
        {
            get { return null; }
        }

        public override double ParagraphIndent
        {
            get { return _paragraphIndent; }
        }

        #endregion

        #region Private Fields

        private FlowDirection _flowDirection;
        private TextAlignment _textAlignment;
        private bool _firstLineInParagraph;
        private bool _alwaysCollapsible;
        private TextRunProperties _defaultTextRunProperties;
        private TextWrapping _textWrap;
        private double _indent;
        private double _paragraphIndent;
        private double _lineHeight;

        #endregion
    }

    public class GenericTextParagraphPropertiesImpl : GenericTextParagraphProperties
    {
        private readonly IList<TextTabProperties> _tabs = new List<TextTabProperties>(10);

        public GenericTextParagraphPropertiesImpl(FlowDirection flowDirection, TextAlignment textAlignment, bool firstLineInParagraph, bool alwaysCollapsible, TextRunProperties defaultTextRunProperties, TextWrapping textWrap, double lineHeight, double indent) : base(flowDirection, textAlignment, firstLineInParagraph, alwaysCollapsible, defaultTextRunProperties, textWrap, lineHeight, indent)
        {
        }

        public GenericTextParagraphPropertiesImpl(FontRendering newRendering, double pixelsPerDip,
            List<TextTabProperties> tabs) : base(newRendering, pixelsPerDip)
        {
            if (tabs != null)
            {
                foreach (var textTabPropertiese in tabs)
                {
                    _tabs.Add(textTabPropertiese);
                }
            }
        }

        public override IList<TextTabProperties> Tabs
        {
            get { return _tabs; }
        }
    }
}