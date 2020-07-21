using System.Globalization;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.TextFormatting;

namespace RoslynCodeControls
{
    public class VeryBasicTextRunProperties : TextRunProperties
    {
        private TextRunProperties _textRunPropertiesImplementation;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="baseProps"></param>
        public VeryBasicTextRunProperties(TextRunProperties baseProps)
        {
            _textRunPropertiesImplementation = baseProps;

        }

        /// <inheritdoc />
        public override Brush BackgroundBrush
        {
            get { return _textRunPropertiesImplementation.BackgroundBrush; }
        }

        /// <inheritdoc />
        public override CultureInfo CultureInfo
        {
            get { return _textRunPropertiesImplementation.CultureInfo; }
        }

        /// <inheritdoc />
        public override double FontHintingEmSize
        {
            get { return _textRunPropertiesImplementation.FontHintingEmSize; }
        }

        /// <inheritdoc />
        public override double FontRenderingEmSize
        {
            get { return _textRunPropertiesImplementation.FontRenderingEmSize; }
        }

        /// <inheritdoc />
        public override Brush ForegroundBrush
        {
            get { return _textRunPropertiesImplementation.ForegroundBrush; }
        }

        /// <inheritdoc />
        public override TextDecorationCollection TextDecorations
        {
            get { return _textRunPropertiesImplementation.TextDecorations; }
        }

        /// <inheritdoc />
        public override TextEffectCollection TextEffects
        {
            get { return _textRunPropertiesImplementation.TextEffects; }
        }

        /// <inheritdoc />
        public override Typeface Typeface
        {
            get { return _textRunPropertiesImplementation.Typeface; }
        }
    }

    /// <summary>
    /// 
    /// </summary>
    public class BasicTextRunProperties : VeryBasicTextRunProperties
    {
        private readonly TextRunProperties _baseProps;
        private Brush _backgroundBrush;
        private Brush _foregroundBrush;
        private FontStyle? _fontStyle;
        private Typeface _typeface;
        private FontFamily _fontFamily;
        private double? _fontRenderingEmSize;
        private double? _fontHintingEmSize=null;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="baseProps"></param>
        public BasicTextRunProperties(TextRunProperties baseProps) : base(baseProps)
        {
            _baseProps = baseProps;
        }

        public bool HasCustomization
        {
            get
            {
                return _backgroundBrush != null || _foregroundBrush != null || _fontStyle.HasValue || _typeface != null;
            }
        }

        /// <inheritdoc />
        public override Typeface Typeface
        {
            get { return _typeface ?? _baseProps.Typeface; }
        }

        /// <inheritdoc />
        public override double FontRenderingEmSize
        {
            get
            {
                return _fontRenderingEmSize.HasValue ? _fontRenderingEmSize.Value : _baseProps.FontRenderingEmSize;
                
            }
        }

        /// <inheritdoc />
        public override double FontHintingEmSize
        {
            get { return _fontHintingEmSize.HasValue ? _fontHintingEmSize.Value : _baseProps.FontHintingEmSize; }
        }

        /// <inheritdoc />
        public override TextDecorationCollection TextDecorations
        {
            get { return _baseProps.TextDecorations; }
        }

        /// <inheritdoc />
        public override Brush ForegroundBrush
        {
            get { return _foregroundBrush ?? _baseProps.ForegroundBrush; }
        }

        /// <inheritdoc />
        public override Brush BackgroundBrush
        {
            get { return _backgroundBrush ?? _baseProps.BackgroundBrush; }
        }

        public void SetBackgroundBrush(Brush backgroundBrush)
        {
            _backgroundBrush = backgroundBrush;
        }

        public void SetForegroundBrush(Brush foregroundBrush)
        {
            _foregroundBrush = foregroundBrush;
        }

        public override CultureInfo CultureInfo
        {
            get { return _baseProps.CultureInfo; }
        }

        public override TextEffectCollection TextEffects
        {
            get { return _baseProps.TextEffects; }
        }

        public FontFamily FontFamily
        {
            get { return _fontFamily ?? _baseProps.Typeface.FontFamily; }
        }

        public void SetFontStyle(FontStyle fontStyle)
        {
            _fontStyle = fontStyle;
            _typeface = new Typeface(FontFamily, _fontStyle.Value, _baseProps.Typeface.Weight,
                _baseProps.Typeface.Stretch);
        }

        public TextRunProperties WithFontFamily(FontFamily family)
        {
            _fontFamily = family;
            return this;
        }

        public TextRunProperties WithForegroundBrush(Brush fg)
        {
            _foregroundBrush = fg;
            return this;
        }

        public void SetFontSize(double d)
        {
            _fontRenderingEmSize = d;
        }
    }
}