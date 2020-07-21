using System;

namespace RoslynCodeControls
{
    public class InputRequest
    {
        private readonly string _text;
        public InputRequestKind Kind { get; }

        public string Text
        {
            get
            {
                return Kind == InputRequestKind.TextInput ? _text : Kind == InputRequestKind.NewLine ? "\r\n" : null;
            }
        }

        public int SequenceId { get; set; }

        public InputRequest(InputRequestKind kind, string text)
        {
            Timestamp = DateTime.Now;
            Kind = kind;
            _text = text;
        }

        public DateTime Timestamp { get;  }

        public InputRequest(InputRequestKind kind)
        {
            Kind = kind;
            Timestamp = DateTime.Now;
        }
        public override string ToString()
        {
            return $"{Kind} " + (Text != null ? $"({Text}) " : "") + $"Seq={SequenceId}";
            // switch (Kind)
            // {
                // case InputRequestKind.TextInput:
                    // return "TextInput (" + Text + ")";
                // case InputRequestKind.NewLine:
                    // return "NewLine";
                // case InputRequestKind.Backspace:
                    // return "Backspace";
            // }

            // return base.ToString();
        }
    }
}