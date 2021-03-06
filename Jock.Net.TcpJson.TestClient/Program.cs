﻿using System;
using System.Net;
using System.Text;

namespace Jock.Net.TcpJson.TestClient
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.Title = "Client";

            var running = true;
            var client = new TcpJsonClient(new System.Net.IPEndPoint(new IPAddress(new byte[] { 127, 0, 0, 1 }), 8013));

            #region New NamedStream Feature in Release 1.0.0.2
            // a named stream can do more custom action
            var stream = client.GetNamedStream("TEST");
            stream.AutoFlush = true;
            #endregion

            client.OnReceive<bool>((b,c) =>
            {
                Console.WriteLine($"Response：{b}");
            })
            .OnStoped(c=>
            {
                Console.WriteLine("Connection Lost.");
                //Environment.Exit(-1);
                System.Threading.Thread.Sleep(2000);
                c.Start();
            });
            client.Start();
            while (running)
            {
                var line = Console.ReadLine();
                client.SendObject(line, () => Console.WriteLine("Sended"));

                #region New NamedStream Feature in Release 1.0.0.2
                stream.WriteByte(1);
                #endregion

                #region New SendBytes Feature in Release 1.0.0.3
                client.SendBytes(Encoding.UTF8.GetBytes(line));
                #endregion

                #region New SendRequest Feature in Release 1.0.1
                var result = client.SendRequest("Time", string.Empty);
                Console.WriteLine(result);
                #endregion
            }
        }
    }
}
