using System.Collections.Concurrent;
using System.Collections.Generic;
using agsXMPP.protocol.client;
using LiveCodingLib.Events;

namespace LiveCodingLib
{
    public interface ILiveCodingService
    {
        ConcurrentBag<KeyValuePair<string, Room>> Rooms { get; }

        void Connect(string username, string password, string hostname, string nickname);
        void Disconnect();
        Room JoinRoom(string roomName, bool loadAllHistory = false);
        void LeaveRoom(Room room);

        Message SendMessage(Room room, string message);
        Message SendMessage(string roomName, string message);

        event MessageEvent MessageEvent;
    }
}