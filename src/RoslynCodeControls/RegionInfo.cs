using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media.TextFormatting;
using JetBrains.Annotations;
using Microsoft.CodeAnalysis;
// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace RoslynCodeControls
{
    /// <summary>
    /// 
    /// </summary>
    public class RegionInfo : INotifyPropertyChanged
    {
        private int _offset;
        private int _length;
        private SyntaxToken? _syntaxToken;
        private SyntaxNode _syntaxNode;
        private SyntaxTrivia? _trivia = default;

        public override string ToString()
        {
            return $"{nameof(Offset)}: {Offset}, {nameof(Length)}: {Length}, {nameof(SyntaxToken)}: {SyntaxToken}, {nameof(TriviaValue)}: {TriviaValue}, {nameof(TextRun)}: {TextRun}, {nameof(BoundingRect)}: {BoundingRect}, {nameof(Key)}: {Key}";
        }

        private TextRun _textRun;
        private Rect _boundingRect;
        private List<CharacterCell> _characters;

        /// <summary>
        /// 
        /// </summary>
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

        /// <summary>
        /// 
        /// </summary>
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

        public SyntaxToken TokenValue => SyntaxToken.GetValueOrDefault();

        /// <summary>
        /// 
        /// </summary>
        public SyntaxToken? SyntaxToken
        {
            get { return _syntaxToken; }
            set
            {
                if (Nullable.Equals(value, _syntaxToken)) return;
                _syntaxToken = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public SyntaxNode SyntaxNode
        {
            get { return _syntaxNode; }
            set
            {
                if (Equals(value, _syntaxNode)) return;
                _syntaxNode = value;
                OnPropertyChanged();
            }
        }

        public SyntaxTrivia TriviaValue => Trivia.GetValueOrDefault();
        /// <summary>
        /// 
        /// </summary>
        public SyntaxTrivia? Trivia
        {
            get { return _trivia; }
            set
            {
                if (Nullable.Equals(value, _trivia)) return;
                _trivia = value;
                OnPropertyChanged();
                OnPropertyChanged("TriviaValue");
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public TextRun TextRun
        {
            get { return _textRun; }
            set
            {
                if (Equals(value, _textRun)) return;
                _textRun = value;
                OnPropertyChanged();
            }
        }

        public LinkedListNode<CharInfo> FirstCharInfo { get; }

        /// <summary>
        /// 
        /// </summary>
        public Rect BoundingRect
        {
            get { return _boundingRect; }
            set
            {
                if (value.Equals(_boundingRect)) return;
                _boundingRect = value;
                OnPropertyChanged();
            }
        }


        /// <summary>
        /// 
        /// </summary>
        public string Key { get; set; }

     

        public SyntaxToken? AttachedToken { get; set; }
        public SyntaxNode AttachedNode { get; set; }
        public SyntaxNode StructuredTrivia { get; set; }
        public TriviaPosition? TriviaPosition { get; set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="textRun"></param>
        /// <param name="boundingRect"></param>
        /// <param name="characters"></param>
        public RegionInfo(TextRun textRun, Rect boundingRect, LinkedListNode<CharInfo> firstCharInfo)
        {
            TextRun = textRun;
            FirstCharInfo = firstCharInfo;
            BoundingRect = new Rect((int)boundingRect.X, (int)boundingRect.Y, (int)boundingRect.Width, (int)boundingRect.Height);
            
        }

        /// <summary>
        /// 
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}