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

        public InputRequest(InputRequestKind kind, string text)
        {
            Kind = kind;
            _text = text;
        }

        public InputRequest(InputRequestKind kind)
        {
            Kind = kind;
        }
    }
}