using System.Windows;
// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace RoslynCodeControls
{
    /// <summary>
    /// 
    /// </summary>
    public class CharacterCell
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="bounds"></param>
        /// <param name="point"></param>
        /// <param name="c"></param>
        public CharacterCell(Rect bounds, Point point, char c)
        {
            Bounds = bounds;
            Point = point;
            Column = (int) point.X;
            Row = (int) point.Y;
            Char = c;
        }

        /// <summary>
        /// 
        /// </summary>
        public Rect Bounds { get; }

        /// <summary>
        /// 
        /// </summary>
        public Point Point { get; }

        /// <summary>
        /// 
        /// </summary>
        public char Char { get; }

        /// <summary>
        /// 
        /// </summary>
        public int Column { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public int Row { get; set; }

        public CharacterCell PreviousCell { get; set; }
        public CharacterCell NextCell { get; set; }
        public RegionInfo Region { get; set; }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"({Row}, {Column}) {Char}";
        }
    }
}