using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

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

            #region New NamedStream Feature in Release 1.0.0.2
            var stream = e.ServerClient.GetNamedStream("TEST");
            var streamWorkThreadRunning = true;
            var streamWorkThread = new Thread(() =>
            {
                while (streamWorkThreadRunning)
                {
                    if (stream.DataAvailable > 0)
                    {
                        Console.WriteLine($"{stream.Name} Revice Byte: {stream.ReadByte()}");
                    }
                }
            });
            streamWorkThread.IsBackground = true;
            streamWorkThread.Start();
            #endregion

            e.ServerClient
                .OnReceive<string>((str, client) =>
                {
                    Console.WriteLine($"{client.Client} say: {str}");
                    client.SendObject(true);
                })
                .OnStoped(client =>
                {
                    #region New NamedStream Feature in Release 1.0.0.2
                    streamWorkThreadRunning = false;
                    #endregion
                    Console.WriteLine($"{e.ServerClient.Session["Id"]} disconnected");
                });
        }
    }
}
