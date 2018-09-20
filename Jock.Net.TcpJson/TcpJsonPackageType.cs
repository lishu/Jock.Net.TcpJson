using System;
using System.Collections.Generic;
using System.Text;

namespace Jock.Net.TcpJson
{
    enum TcpJsonPackageType : byte
    {
        Command,
        Json,
        Ping
    }
}
