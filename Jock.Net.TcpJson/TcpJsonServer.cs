using System;
using System.ComponentModel;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace Jock.Net.TcpJson
{
    /// <summary>
    /// Json 通讯服务端
    /// </summary>
    public class TcpJsonServer : SafeThreadObject
    {
        private IPEndPoint mListenerPoint;
        private TcpListener mListener;
        private WeakList<TcpJsonServerClient> mClients = new WeakList<TcpJsonServerClient>();

        /// <summary>
        /// 创建一个 Json 通讯服务端
        /// </summary>
        /// <param name="listenerPoint">侦听端口</param>
        public TcpJsonServer(IPEndPoint listenerPoint)
        {
            mListenerPoint = listenerPoint;
        }

        /// <summary>
        /// 等待下一个连接的测试间隔（微秒）
        /// </summary>
        public int WaitPendingTime { get; set; } = 50;

        /// <summary>
        /// 当一个远程客户端连接时发生，可以设置 Cancel 属性取消连接
        /// </summary>
        public event ConnectingEventHandler Connecting;

        /// <summary>
        /// 当为远程客户端建立 <c>TcpJsonClient</c> 发生
        /// </summary>
        public event ConnectedEventHandler Connected;

        /// <summary>
        /// 内部线程运行代码
        /// </summary>
        /// <param name="token">当用户调用 Stop 方法时触发取消通知</param>
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

    /// <summary>
    /// 提供新客户端连接时的回调
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    public delegate void ConnectingEventHandler(object sender, ConnectingEventArgs e);

    /// <summary>
    /// 新客户端连接时回调参数
    /// </summary>
    public class ConnectingEventArgs : CancelEventArgs
    {
        /// <summary>
        /// 与客户端建立连接的 <c>TcpClient</c> 对象
        /// </summary>
        public TcpClient Client { get; }

        internal ConnectingEventArgs(TcpClient tcpClient)
        {
            Client = tcpClient;
        }
    }

    /// <summary>
    /// 提供客户端完成连接时回调
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    public delegate void ConnectedEventHandler(object sender, ConnectedEventArgs e);

    /// <summary>
    /// 客户端完成连接时回调参数
    /// </summary>
    public class ConnectedEventArgs : EventArgs
    {
        /// <summary>
        /// 客户通讯端对象
        /// </summary>
        public TcpJsonServerClient ServerClient { get; }

        internal ConnectedEventArgs(TcpJsonServerClient serverClient)
        {
            ServerClient = serverClient;
        }
    }
}
