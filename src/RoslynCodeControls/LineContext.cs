using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media.TextFormatting;

namespace RoslynCodeControls
{
    /// <summary>
    /// 
    /// </summary>
    public class LineContext
    {
        private int _textStorePosition;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="maxX"></param>
        /// <param name="textLineAction"></param>
        /// <param name="myTextLine"></param>
        /// <param name="lineOriginPoint"></param>
        /// <param name="curCellRow"></param>
        /// <param name="lineNumber"></param>
        /// <param name="textStorePosition"></param>
        public LineContext(double maxX, Action<TextLine> textLineAction, TextLine myTextLine, Point lineOriginPoint, int curCellRow, int lineNumber, int textStorePosition)
        {
            MaxX = maxX;
            TextLineAction = textLineAction;
            MyTextLine = myTextLine;
            LineOriginPoint = lineOriginPoint;
            CurCellRow = curCellRow;
            LineNumber = lineNumber;
            TextStorePosition = textStorePosition;
        }

        public LineContext()
        {
            
        }

        /// <summary>
        /// 
        /// </summary>
        public double MaxX { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public Action<TextLine> TextLineAction { get; private set; }
        /// <summary>
        /// 
        /// </summary>
        public TextLine MyTextLine { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public Point LineOriginPoint { get; set; } = new Point(0, 0);
        /// <summary>
        /// 
        /// </summary>
        public int CurCellRow { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int LineNumber { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public int TextStorePosition
        {
            get { return _textStorePosition; }
            set { _textStorePosition = value; }
        }

        /// <summary>
        /// 
        /// </summary>
        public LineInfo LineInfo { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public IList<string> LineParts { get; set; } = new List<string>();

        public IList<int> Offsets { get; set; } = new List<int>();
        public double MaxY { get; set; }
    }
}