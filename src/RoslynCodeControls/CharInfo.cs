namespace RoslynCodeControls
{
    public class CharInfo
    {
        public int LineIndex { get; }
        public int RunIndex { get; }
        public char Character { get; }
        public double AdvanceWidth { get; }
        public bool? CaretStop { get; }
        public double XOrigin { get; }
        public double YOrigin { get; }
        public int Index { get; set; }
        public int LineNumber { get; set; }

        public CharInfo(in int lineNo, in int index, in int lineIndex, in int runIndex, char character,
            double advanceWidth,
            bool? caretStop,
            double xOrigin, double yOrigin)
        {
            LineNumber = lineNo;
            Index = index;
            LineIndex = lineIndex;
            RunIndex = runIndex;
            Character = character;
            AdvanceWidth = advanceWidth;
            CaretStop = caretStop;
            XOrigin = xOrigin;
            YOrigin = yOrigin;
        }
    }
}