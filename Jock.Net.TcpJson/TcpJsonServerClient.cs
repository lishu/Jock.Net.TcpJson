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
    /// Json 服务端的通讯端
    /// </summary>
    public class TcpJsonServerClient : TcpJsonClient
    {
        internal TcpJsonServerClient(TcpJsonServer server, TcpClient tcpClient) : base(tcpClient)
        {
            Server = server;
        }

        /// <summary>
        /// 获取相关的服务端
        /// </summary>
        public TcpJsonServer Server { get; }
    }
}
