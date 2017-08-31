using agsXMPP.protocol.client;

namespace LiveCodingLib.Events.Args
{
    public class MessageEventArgs : BaseArgs
    {
        public Message Message { get; set; }
    }
}
