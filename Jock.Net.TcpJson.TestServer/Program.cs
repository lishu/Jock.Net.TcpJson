using System;
using System.Net;

namespace Jock.Net.TcpJson.TestServer
{
    class Program
    {
        static int SessionId = 0;

        static void Main(string[] args)
        {
            var server = new TcpJsonServer(new System.Net.IPEndPoint(IPAddress.Any, 8013));
            server.Connected += Server_Connected;
            server.Start();
        }

        private static void Server_Connected(object sender, ConnectedEventArgs e)
        {
            e.ServerClient.Session["Id"] = $"{++SessionId}";
            Console.WriteLine($"{e.ServerClient.Session["Id"]} is connected.");
            e.ServerClient
                .OnReceive<string>((str, client) =>
                {
                    Console.WriteLine($"{client.Client} say: {str}");
                    client.SendObject(true);
                })
                .OnStoped(client =>
                {
                    Console.WriteLine($"{e.ServerClient.Session["Id"]} disconnected");
                });
        }
    }
}
