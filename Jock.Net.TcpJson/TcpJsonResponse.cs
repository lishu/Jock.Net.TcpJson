using System;

namespace Jock.Net.TcpJson
{
    class TcpJsonResponse<T>
    {
        public Guid Id { get; set; }
        public T Object { get; set; }
    }
}
