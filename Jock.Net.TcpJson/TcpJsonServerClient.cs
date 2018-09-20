using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace Jock.Net.TcpJson
{
    public class TcpJsonServerClient : TcpJsonClient
    {
        public TcpJsonServerClient(TcpJsonServer server, TcpClient tcpClient) : base(tcpClient)
        {
            Server = server;
        }

        public TcpJsonServer Server { get; }
    }
}
