using agsXMPP;
using agsXMPP.protocol.client;
using agsXMPP.protocol.extensions.chatstates;
using System.Collections.Generic;

namespace LiveCodingLib
{
    public class Room
    {
        public Jid Jid { get; set; }
        public List<string> Users { get; set; } = new List<string>();
        public List<Message> Messages { get; set; } = new List<Message>();
        public bool Online { get; set; } = false;
        public ErrorCondition ErrorCondition { get; set; } = (ErrorCondition)(999);
        public Chatstate Chatstate { get; set; }
        public NotifyMode Notify { get; set; }

        public string Roomname
        {
            get
            {
                return Jid.Bare;
            }
        }
    }
}
