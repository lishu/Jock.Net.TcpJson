using System;
using System.Collections.Generic;
using System.Text;

namespace Jock.Net.TcpJson
{
    class TcpJsonResponse<T>
    {
        public Guid Id { get; set; }
        public T Object { get; set; }
    }
}
