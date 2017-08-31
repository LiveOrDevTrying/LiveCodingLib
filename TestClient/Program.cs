using agsXMPP.protocol.client;
using LiveCodingLib;
using System;

namespace TestClient
{
    class Program
    {
        private static ILiveCodingService _liveCodingService;

        static void Main(string[] args)
        {
            _liveCodingService = new LiveCodingService();
            _liveCodingService.MessageEvent += OnMessage;

            // Nickname must be the same as username but may be formatted
            _liveCodingService.Connect("username", "password", "livecoding.tv", "nickname");

            while (true)
            {
                var outmessage = Console.ReadLine();

                // This is the format for sending a message
                //_jabber.SendMessage("liveordevtrying", "chat.livecoding.tv", outmessage);
            }
        }

        private static void OnMessage(object e, Message args)
        {
            Console.WriteLine(args.From.Bare + " - " + args.From.Resource + ": " + args.Body);
        }
    }
}
