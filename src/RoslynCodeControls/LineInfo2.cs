using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using JetBrains.Annotations;

namespace RoslynCodeControls
{
    public class LineInfo2
    {
        public static LineInfo2 None = new LineInfo2(-1, null, 0, default, default, default);

        public LineInfo2(int lineNumber, LinkedListNode<CharInfo> firstCharInfo, int offset, Point origin,
            double height, int length)
        {
            LineNumber = lineNumber;
            FirstCharInfo = firstCharInfo;
            Offset = offset;
            Origin = origin;
            Height = height;
            Length = length;
        }

        public int LineNumber { get; }

        public LinkedListNode<CharInfo> FirstCharInfo { get; set; }

        public int Offset { get; }
        
        public Point Origin { get; }
     
        public double Height { get; set; }

        public int Length { get; set; }


    }
}