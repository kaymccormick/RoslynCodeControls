using System.Collections.Generic;
using System.Windows;

namespace RoslynCodeControls
{
    /// <summary>
    /// 
    /// </summary>
    public class LineInfo
    {
        /// <summary>
        /// 
        /// </summary>
        public int LineNumber { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int Offset { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int Length { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string Text { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public List<RegionInfo> Regions { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public Size Size { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public Point Origin { get; set; }

        public LineInfo NextLine { get; set; }
        public LineInfo PrevLine { get; set; }
        public double Height { get; set; }

        public override string ToString()
        {
            return $"{nameof(LineNumber)}: {LineNumber}, {nameof(Offset)}: {Offset}, {nameof(Length)}: {Length}, {nameof(Text)}: {Text}, {nameof(Regions)}: {Regions}, {nameof(Size)}: {Size}, {nameof(Origin)}: {Origin}";
        }
    }
}