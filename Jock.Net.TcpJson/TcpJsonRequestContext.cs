using System;
using System.Threading;

namespace Jock.Net.TcpJson
{
    class TcpJsonRequestContext
    {
        public TcpJsonRequestContext(TcpJsonClient tcpJsonClient, Guid id)
        {
            this.tCreateTime = DateTime.Now;
            this.Client = tcpJsonClient;
            this.Id = id;
            this.WaitHandler = new AutoResetEvent(false);
        }

        private DateTime tCreateTime;

        public TcpJsonClient Client { get; private set; }
        public Guid Id { get; private set; }
        public AutoResetEvent WaitHandler { get; }
        public object Response { get; private set; }
        public bool IsError { get; private set; }
        public string ErrorMessage { get; private set; }

        public void SetResponse(object response)
        {
            Response = response;
            this.WaitHandler.Set();
        }

        public void SetError(string error)
        {
            IsError = true;
            ErrorMessage = error;
            this.WaitHandler.Set();
        }
    }
}
