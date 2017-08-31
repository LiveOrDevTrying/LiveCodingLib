using agsXMPP;
using agsXMPP.protocol.client;
using agsXMPP.protocol.extensions.chatstates;
using agsXMPP.protocol.x.muc;
using agsXMPP.Xml.Dom;
using LiveCodingLib.Events;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace LiveCodingLib
{
    public class LiveCodingService : ILiveCodingService
    {
        private XmppClientConnection _xmpp;
        private ConcurrentBag<KeyValuePair<string, Room>> _rooms;
        private string _nickname;
        private string _hostName;

        private event MessageEvent _messageEvent;

        public LiveCodingService()
        {
        }

        public void Connect(string username, string password, string hostname, string nickname)
        {
            if (_xmpp == null)
            {
                _nickname = nickname;
                _hostName = hostname;
                _rooms = new ConcurrentBag<KeyValuePair<string, Room>>();

                _xmpp = new XmppClientConnection();
                _xmpp.OnAuthError += OnAuthError;
                _xmpp.OnXmppConnectionStateChanged += OnXmppConnectionStateChanged;
                _xmpp.OnSocketError += OnSocketError;
                _xmpp.OnError += OnError;
                _xmpp.OnLogin += OnLogin;
                _xmpp.OnIq += OnIq;
                _xmpp.OnPresence += OnPresence;
                _xmpp.OnMessage += OnMessage;

                _xmpp.Server = hostname;
                _xmpp.AutoResolveConnectServer = true;
                _xmpp.EnableCapabilities = true;
                _xmpp.KeepAliveInterval = 1;

                _xmpp.Open(username, password, string.Empty, 0);
            }
        }
        public void Disconnect()
        {
            _rooms = null;
            _xmpp.SocketDisconnect();

        }
        public Message SendMessage(Room room, string message)
        {
            var msg = new Message(room.Roomname, _xmpp.MyJID, MessageType.groupchat, message)
            {
                Id = Guid.NewGuid().ToString(),
                Chatstate = Chatstate.active
            };

            _xmpp.Send(msg);

            return msg;
        }
        public Room JoinRoom(string roomName, bool loadAllHistory = false)
        {
            if (!_rooms.Any(s => s.Key.Trim().ToLower() == roomName.Trim().ToLower()))
            {
                var room = new Room()
                {
                    Chatstate = Chatstate.active,
                    Notify = NotifyMode.Always,
                    Jid = new Jid(roomName)
                };
                room.Jid.Resource = _nickname;
                _rooms.Add(new KeyValuePair<string, Room>(room.Roomname, room));

                var mucPresence = new Presence()
                {
                    To = room.Jid
                };

                _xmpp.Send(mucPresence);

                Console.WriteLine("-> " + mucPresence.ToString());

                return room;
            }
            return null;
        }
        public void LeaveRoom(Room room)
        {
            room.Online = false;

            var mucPresence = new Presence()
            {
                To = room.Jid,
                Type = PresenceType.unavailable
            };

            _xmpp.Send(mucPresence);
        }

        private void OnIq(object sender, IQ iq)
        {
            if (iq.Type == IqType.get)
            {
                var ping = iq.SelectSingleElement("ping", "urn:xmpp:ping");
                if (ping != null)
                {
                    var pong = new agsXMPP.protocol.client.IQ(agsXMPP.protocol.client.IqType.result, iq.To, iq.From);
                    pong.Id = iq.Id;
                    _xmpp.Send(pong);
                }
            }
        }
        private void OnBindError(object sender, Element e)
        {
            Console.WriteLine("BIND ERR: " + e.ToString());
            // Reconnect here
        }
        private void OnSocketError(object sender, Exception ex)
        {
            Console.WriteLine("SOCKET ERR: " + ex.Message);
            // Reconnect here
        }
        private void OnXmppConnectionStateChanged(object sender, XmppConnectionState state)
        {
            Console.WriteLine("Conn State Changed: " + state.ToString());
            if (state == XmppConnectionState.Disconnected)
            {
                // Reconnect here
            }
        }
        private void OnError(object sender, Exception ex)
        {
            Console.WriteLine("OnError:\t" + ex.Message);
            Console.WriteLine(ex.ToString());
            Console.WriteLine("---------");
        }
        private void OnAuthError(object sender, Element e)
        {
            Console.WriteLine("Error while logging in to the server: " + e.ToString());
        }
        private void OnMessage(object sender, Message msg)
        {
            if (_messageEvent != null)
            {
                _messageEvent(this, msg);
            }

            var room = _rooms.Where(s => s.Key.Trim().ToLower() == msg.From.Bare.Trim().ToLower()).SingleOrDefault().Value;

            if (room == null)
            {
                var pair = new KeyValuePair<string, Room>(msg.From.Bare, new Room()
                {
                    Chatstate = Chatstate.active,
                    Notify = NotifyMode.Always,
                    Jid = new Jid(msg.From.Bare)
                });

                pair.Value.Jid.Resource = _nickname;
                _rooms.Add(pair);
            }
            room.Messages.Add(msg);
        }
        private void OnPresence(object sender, Presence pres)
        {
            if (pres.HasTag(typeof(User), true) ||
                (pres.Type == PresenceType.error && pres.HasTag(typeof(Muc), true)))
            {
                OnMucPresence(pres);
            }
        }
        private void OnLogin(object sender)
        {
            _xmpp.SendMyPresence();

            // Join rooms now
        }
        private void OnMucSelfPresence(Room room, Presence pres, Element xChild)
        {
            room.Online = true;
        }
        private void OnMucPresence(Presence pres)
        {
            // A presence was received regarding a room we didn't join
            var roomname = pres.From.Bare;
            if (!_rooms.Any(s => s.Key.Trim().ToLower() == roomname.Trim().ToLower()))
            {
                _rooms.Add(new KeyValuePair<string, Room>(roomname, new Room()
                {
                    Jid = pres.From
                }));
                return;
            }

            var room = _rooms.Where(s => s.Key.Trim().ToLower() == roomname.Trim().ToLower()).SingleOrDefault().Value;
            room.ErrorCondition = (ErrorCondition)(999);
            if (pres.Type == PresenceType.error)
            {
                room.ErrorCondition = pres.Error.Condition;
                return;
            }

            var xChild = pres.SelectSingleElement("x", "http://jabber.org/protocol/muc#user");
            foreach (Element el in xChild.SelectElements("status"))
            {
                if (el.GetAttribute("code") == "110")
                {
                    OnMucSelfPresence(room, pres, xChild);
                    break;
                }
                else if (el.GetAttribute("code") == "210")
                {
                    // rename by server
                    room.Jid.Resource = pres.From.Resource;
                    break;
                }
            }

            string online = "online";

            if (pres.HasTag("show"))
            {
                online = pres.GetTag("show");
            }

            if (pres.Type == PresenceType.unavailable)
            {
                online = "off";
            }

            string statusStr = "";

            if (pres.HasTag("status"))
            {
                statusStr = pres.GetTag("status");
            }

            string jid = "";

            if (xChild.HasTag("item"))
            {
                jid = xChild.SelectSingleElement("item").GetAttribute("jid");
            }
        }

        protected virtual void SendChatState(Room room, Chatstate state)
        {
            if (room.Chatstate != state)
            {
                room.Chatstate = state;
                var msg = new Message(new Jid(room.Roomname), _xmpp.MyJID)
                {
                    Type = MessageType.groupchat,
                    Chatstate = state
                };
                _xmpp.Send(msg);
            }
        }

        public event MessageEvent MessageEvent
        {
            add
            {
                _messageEvent += value;
            }
            remove
            {
                _messageEvent -= value;
            }
        }
        public ConcurrentBag<KeyValuePair<string, Room>> Rooms
        {
            get
            {
                return _rooms;
            }
        }
    }
}