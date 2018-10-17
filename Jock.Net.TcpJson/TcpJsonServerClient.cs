using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace Jock.Net.TcpJson
{
    /// <summary>
    /// The communication side of the Json server
    /// </summary>
    public class TcpJsonServerClient : TcpJsonClient
    {
        internal TcpJsonServerClient(TcpJsonServer server, TcpClient tcpClient) : base(tcpClient)
        {
            Server = server;
        }

        /// <summary>
        /// Get the relevant service side <c>TcpJsonServer</c>
        /// </summary>
        public TcpJsonServer Server { get; }
    }
}
