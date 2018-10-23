using System;
using System.Collections.Generic;
using System.Text;

namespace Jock.Net.TcpJson
{
    class TcpJsonRequest<T> : TcpJsonRequest
    {
        public new T Object {
            get
            {
                return (T)base.Object;
            }
            set
            {
                base.Object = value;
            }
        }
    }

    class TcpJsonRequest
    {
        public Guid Id { get; set; }
        public string URI { get; set; }
        public object Object { get; set; }
    }
}
