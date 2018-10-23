using System;
using System.Collections.Generic;
using System.Text;

namespace Jock.Net.TcpJson
{
    enum TcpJsonCookieSyncAction : byte
    {
        Clear = 0,
        Add = 1,
        Remove = 2,
        Update = 3
    }
}
