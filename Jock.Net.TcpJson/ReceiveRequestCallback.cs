using System;

namespace Jock.Net.TcpJson
{
    class ReceiveRequestCallback
    {
        public string URI;
        public Type RequestType;
        public Type ResponseType;
        public Delegate Callback;

        public ReceiveRequestCallback(string uri, Type requestType, Type responseType, Delegate callback)
        {
            this.URI = uri;
            this.RequestType = requestType;
            this.ResponseType = responseType;
            this.Callback = callback;
        }
    }
}