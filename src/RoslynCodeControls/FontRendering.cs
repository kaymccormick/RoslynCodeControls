using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Media;
using JetBrains.Annotations;

namespace RoslynCodeControls
{
    /// <summary>
    /// 
    /// </summary>
    public class FontRendering
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="emSize"></param>
        /// <param name="alignment"></param>
        /// <param name="decorations"></param>
        /// <param name="textColor"></param>
        /// <param name="face"></param>
        /// <returns></returns>
        public static FontRendering CreateInstance(double emSize, TextAlignment alignment, TextDecorationCollection decorations, Brush textColor, Typeface face)
        {
            return new FontRendering(emSize, alignment, decorations, textColor, face);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="emSize"></param>
        /// <param name="alignment"></param>
        /// <param name="decorations"></param>
        /// <param name="textColor"></param>
        /// <param name="face"></param>
        /// <exception cref="InvalidEnumArgumentException"></exception>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        private FontRendering(
            double emSize,
            TextAlignment alignment,
            TextDecorationCollection decorations,
            [NotNull] Brush textColor,
            [NotNull] Typeface face)
        {
            if (!Enum.IsDefined(typeof(TextAlignment), alignment))
                throw new InvalidEnumArgumentException(nameof(alignment), (int) alignment, typeof(TextAlignment));
            if (emSize <= 0) throw new ArgumentOutOfRangeException(nameof(emSize));
            _fontSize = emSize;
            _alignment = alignment;
            _textDecorations = decorations;
            _textColor = textColor ?? throw new ArgumentNullException(nameof(textColor));
            _typeface = face ?? throw new ArgumentNullException(nameof(face));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public double FontSize
        {
            get { return _fontSize; }
            set
            {
                if (value <= 0)
                    throw new ArgumentOutOfRangeException("value", "Parameter Must Be Greater Than Zero.");
                if (double.IsNaN(value))
                    throw new ArgumentOutOfRangeException("value", "Parameter Cannot Be NaN.");
                _fontSize = value;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public TextAlignment TextAlignment
        {
            get { return _alignment; }
            set { _alignment = value; }
        }

        /// <summary>
        /// 
        /// </summary>
        public TextDecorationCollection TextDecorations
        {
            get { return _textDecorations; }
            set { _textDecorations = value; }
        }

        /// <summary>
        /// 
        /// </summary>
        public Brush TextColor
        {
            get { return _textColor; }
            set { _textColor = value; }
        }

        /// <summary>
        /// 
        /// </summary>
        public Typeface Typeface
        {
            get { return _typeface; }
            set { _typeface = value; }
        }

        private double _fontSize;
        private TextAlignment _alignment;
        private TextDecorationCollection _textDecorations;
        private Brush _textColor;
        private Typeface _typeface;
    }
}