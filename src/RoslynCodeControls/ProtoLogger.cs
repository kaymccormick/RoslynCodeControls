using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using JetBrains.Annotations;

namespace RoslynCodeControls
{
    internal sealed class ProtoLogger
    {
        private readonly Func<object, byte[]> _getBytes;
        private readonly IPEndPoint _ipEndPoint;

        // private readonly Layout _layout;
        private readonly UdpClient _udpClient;

        private static ProtoLogger _instance;


        // public Layout XmlEventLayout { get; }

        [NotNull]
        public static ProtoLogger Instance
        {
            get
            {
                if (_instance == null) _instance = new ProtoLogger();

                return _instance;
            }
        }

        private static UdpClient CreateUdpClient()
        {
            return new UdpClient
            {
                EnableBroadcast = true,
                Client =
                    new Socket(SocketType.Dgram, ProtocolType.Udp)
                    {
                        EnableBroadcast = true,
                        DualMode = true
                    }
            };
        }

        public ProtoLogger()
        {
            // Log4JXmlEventLayoutRenderer xmlEventLayoutRenderer = new MyLog4JXmlEventLayoutRenderer();
            // XmlEventLayout = new MyLayout(xmlEventLayoutRenderer);
            _udpClient = CreateUdpClient();
            _ipEndPoint = new IPEndPoint(_protoLogIpAddress, _protoLogPort);

            // _layout = XmlEventLayout;
            // if (_layout == null)
            // {
            // throw new AppInvalidOperationException("LAyout is null");
            // }

            _getBytes = DefaultGetBytes;
        }


        [NotNull]
        private byte[] DefaultGetBytes(object arg)
        {
        var encoding = Encoding.UTF8;
        return encoding.GetBytes(arg.ToString() ?? string.Empty);
        }

        public void LogAction(object info)
        {
        var bytes = _getBytes(info);
        var nBytes = bytes.Length;
        _udpClient.Send(bytes, nBytes, _ipEndPoint);
        }

        // public static readonly Action<LogEventInfo> ProtoLogAction = Instance.LogAction;
        

        private int _protoLogPort=17770;
        private IPAddress _protoLogIpAddress = IPAddress.Parse("10.25.0.102");

        /// <summary>
        /// </summary>
        // public static LogDelegates.LogMethod ProtoLogDelegate { get; } = ProtoLogMessage;

        // private static void ProtoLogMessage(string message)
        // {
        // ProtoLogAction(
        // LogEventInfo.Create(
        // LogLevel.Warn
        // , typeof(AppLoggingConfigHelper).FullName
        // , message
        // )
        // );
        // }
    }
}