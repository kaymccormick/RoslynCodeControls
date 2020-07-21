using System;
using System.Windows.Media;
using System.Windows.Media.TextFormatting;

namespace RoslynCodeControls
{
    /// <summary>
    /// 
    /// </summary>
    public interface ICustomTextSource
    {
        /// <summary>
        /// 
        /// </summary>
        int Length { get; }

    
        // FontRendering FontRendering { get; set; }

        // FontFamily Family { get; set; }
        /// <summary>
        /// 
        /// </summary>
        double EmSize { get; set; }
        /// <summary>
        /// 
        /// </summary>
        GenericTextRunProperties BaseProps { get; }

        /// <summary>
        /// 
        /// </summary>
        int EolLength { get; }

    
        /// <summary>
        /// 
        /// </summary>
        double PixelsPerDip { get; set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="textSourceCharacterIndex"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        TextRun GetTextRun(int textSourceCharacterIndex);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="textSourceCharacterIndexLimit"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        TextSpan<CultureSpecificCharacterBufferRange> GetPrecedingText(
            int textSourceCharacterIndexLimit);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="textSourceCharacterIndex"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        int GetTextEffectCharacterIndexFromTextSourceCharacterIndex(int textSourceCharacterIndex);

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        // BasicTextRunProperties BasicProps();

        /// <summary>
        /// 
        /// </summary>
        // void GenerateText();
        /// <summary>
        /// 
        /// </summary>
        /// <param name="arg"></param>
        /// <param name="text"></param>
        /// <returns></returns>
        TextRunProperties MakeProperties(object arg, string text);

        void Init();
            
    }
}