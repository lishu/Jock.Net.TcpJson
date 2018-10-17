using System;
using System.Collections.Generic;
using System.Text;

namespace Jock.Net.TcpJson
{
    enum TcpJsonPackageType : byte
    {
        Command = 0,
        Json = 1,
        Ping = 2,
        NamedStream = 3,
        Bytes = 4
    }
}
