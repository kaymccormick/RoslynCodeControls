using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using JetBrains.Annotations;

namespace RoslynCodeControls
{
    public class LineInfo2 : INotifyPropertyChanged
    {
        private int _length;
        private double _height;
        private int _lineNumber;
        private int _offset;
        private Point _origin;

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

        public int LineNumber
        {
            get { return _lineNumber; }
            set
            {
                if (value == _lineNumber) return;
                _lineNumber = value;
                OnPropertyChanged();
            }
        }

        public LinkedListNode<CharInfo> FirstCharInfo { get; }

        public int Offset
        {
            get { return _offset; }
            set
            {
                if (value == _offset) return;
                _offset = value;
                OnPropertyChanged();
            }
        }

        public Point Origin
        {
            get { return _origin; }
            set
            {
                if (value.Equals(_origin)) return;
                _origin = value;
                OnPropertyChanged();
            }
        }

        public double Height
        {
            get { return _height; }
            set
            {
                if (value.Equals(_height)) return;
                _height = value;
                OnPropertyChanged();
            }
        }

        public int Length
        {
            get { return _length; }
            set
            {
                if (value == _length) return;
                _length = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}