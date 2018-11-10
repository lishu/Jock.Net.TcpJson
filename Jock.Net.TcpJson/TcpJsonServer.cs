using System;
using System.ComponentModel;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace Jock.Net.TcpJson
{
    /// <summary>
    /// Json Communication Service Side
    /// </summary>
    public class TcpJsonServer : SafeThreadObject
    {
        private IPEndPoint mListenerPoint;
        private TcpListener mListener;
        private WeakList<TcpJsonServerClient> mClients = new WeakList<TcpJsonServerClient>();

        /// <summary>
        ///Create a Json Communication server
        /// </summary>
        /// <param name="listenerPoint">Listening port</param>
        public TcpJsonServer(IPEndPoint listenerPoint)
        {
            mListenerPoint = listenerPoint;
        }

        /// <summary>
        /// Test interval to wait for next connection (microseconds)
        /// </summary>
        public int WaitPendingTime { get; set; } = 50;

        /// <summary>
        /// When a remote client connection occurs, you can set the <c>Cancel</c> property to cancel the connection
        /// </summary>
        public event ConnectingEventHandler Connecting;

        /// <summary>
        /// Triggered when a remote client connection is complete
        /// </summary>
        public event ConnectedEventHandler Connected;

        /// <summary>
        /// Internal thread Run code
        /// </summary>
        /// <param name="token">Triggering a cancellation notification when the user calls the Stop method</param>
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

            serverClient.Start();

            var e = new ConnectedEventArgs(serverClient);
            Connected?.Invoke(this, e);
        }

        private void ServerClient_Stoped(object sender, EventArgs e)
        {
            mClients.Remove((TcpJsonServerClient)sender);
        }
    }

    /// <summary>
    /// Callback when providing a new client connection
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    public delegate void ConnectingEventHandler(object sender, ConnectingEventArgs e);

    /// <summary>
    /// Callback parameters when a new client connects
    /// </summary>
    public class ConnectingEventArgs : CancelEventArgs
    {
        /// <summary>
        /// <c>TcpClient</c> object to which the client establishes a connection
        /// </summary>
        public TcpClient Client { get; }

        internal ConnectingEventArgs(TcpClient tcpClient)
        {
            Client = tcpClient;
        }
    }

    /// <summary>
    /// Provides callbacks when a client completes a connection
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    public delegate void ConnectedEventHandler(object sender, ConnectedEventArgs e);

    /// <summary>
    /// Callback parameters when the client finishes the connection
    /// </summary>
    public class ConnectedEventArgs : EventArgs
    {
        /// <summary>
        /// Client Communication End Object
        /// </summary>
        public TcpJsonServerClient ServerClient { get; }

        internal ConnectedEventArgs(TcpJsonServerClient serverClient)
        {
            ServerClient = serverClient;
        }
    }
}
