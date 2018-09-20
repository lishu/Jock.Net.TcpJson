using System;
using System.ComponentModel;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace Jock.Net.TcpJson
{
    public class TcpJsonServer : SafeThreadObject
    {
        private IPEndPoint mListenerPoint;
        private TcpListener mListener;
        private WeakList<TcpJsonServerClient> mClients = new WeakList<TcpJsonServerClient>();

        public TcpJsonServer(IPEndPoint listenerPoint)
        {
            mListenerPoint = listenerPoint;
        }

        public int WaitPendingTime { get; set; } = 50;

        public event ConnectingEventHandler Connecting;
        public event ConnectedEventHandler Connected;

        protected override void DoRun(CancellationToken token)
        {
            mListener = new TcpListener(mListenerPoint);
            mListener.Start();
            try
            {
                while (!token.IsCancellationRequested)
                {
                    if (mListener.Pending())
                    {
                        var tcpClient = mListener.AcceptTcpClient();
                        var e = new ConnectingEventArgs(tcpClient);
                        Connecting?.Invoke(this, e);
                        if (e.Cancel)
                        {
                            tcpClient.Close();
                            continue;
                        }
                        CreateNewServerClient(tcpClient);
                    }
                    else
                    {
                        Thread.Sleep(WaitPendingTime);
                    }
                }
            }
            finally
            {
                mListener.Stop();
            }
        }

        private void CreateNewServerClient(TcpClient tcpClient)
        {
            var serverClient = new TcpJsonServerClient(this, tcpClient);
            serverClient.Stoped += ServerClient_Stoped;
            mClients.Add(serverClient);
            var e = new ConnectedEventArgs(serverClient);
            Connected?.Invoke(this, e);
            e.ServerClient.Start();
        }

        private void ServerClient_Stoped(object sender, EventArgs e)
        {
            mClients.Remove((TcpJsonServerClient)sender);
        }
    }

    public delegate void ConnectingEventHandler(object sender, ConnectingEventArgs e);

    public class ConnectingEventArgs : CancelEventArgs
    {
        public TcpClient Client { get; }

        internal ConnectingEventArgs(TcpClient tcpClient)
        {
            Client = tcpClient;
        }
    }

    public delegate void ConnectedEventHandler(object sender, ConnectedEventArgs e);

    public class ConnectedEventArgs : EventArgs
    {
        public TcpJsonServerClient ServerClient { get; }

        internal ConnectedEventArgs(TcpJsonServerClient serverClient)
        {
            ServerClient = serverClient;
        }
    }
}
