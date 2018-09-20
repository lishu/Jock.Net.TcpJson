using System;
using System.Net;

namespace Jock.Net.TcpJson.TestClient
{
    class Program
    {
        static void Main(string[] args)
        {
            var client = new TcpJsonClient(new System.Net.IPEndPoint(new IPAddress(new byte[] { 10, 0, 0, 2 }), 8013));
            client.OnReceive<bool>((b,c) =>
            {
                Console.WriteLine($"已回复：{b}");
            });
            client.Start();
            while (true)
            {
                var line = Console.ReadLine();
                client.SendObject(line, () => Console.WriteLine("已发送"));
            }
        }
    }
}
