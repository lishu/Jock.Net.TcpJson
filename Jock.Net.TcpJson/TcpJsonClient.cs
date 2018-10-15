using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace Jock.Net.TcpJson
{
    /// <summary>
    /// Json 通讯客户端
    /// </summary>
    public class TcpJsonClient : SafeThreadObject
    {
        private Queue<TcpJsonPackage> sendPackageQueue = new Queue<TcpJsonPackage>();
        private MemoryStream readCache = new MemoryStream();
        private DateTime mRemoteActiveTime;
        private List<Action<string>> mCommandHandlers = new List<Action<string>>();
        private List<JsonCallback> mJsonHandlers = new List<JsonCallback>();
        private List<Action<TcpJsonClient>> mStopHandlers = new List<Action<TcpJsonClient>>();
        private List<TcpJsonNamedStream> mNamedStreams = new List<TcpJsonNamedStream>();

        /// <summary>
        /// 连接到指定的服务端
        /// </summary>
        /// <param name="remoteEP">要连接到的服务端点</param>
        public TcpJsonClient(IPEndPoint remoteEP)
        {
            Client = new TcpClient();
            Client.Connect(remoteEP);
            this.mRemoteActiveTime = DateTime.Now;
        }

        internal TcpJsonClient(TcpClient tcpClient)
        {
            this.Client = tcpClient;
            this.mRemoteActiveTime = DateTime.Now;
        }

        /// <summary>
        /// 获取一个命名流对象
        /// </summary>
        /// <param name="name">流名称</param>
        /// <returns>命名流</returns>
        public TcpJsonNamedStream GetNamedStream(string name)
        {
            lock (mNamedStreams)
            {
                var found = mNamedStreams.FirstOrDefault(s => s.Name == name);
                if(found != null)
                {
                    return found;
                }
                mNamedStreams.Add(found = new TcpJsonNamedStream(this) { Name = name });
                return found;
            }
        }

        internal void FlushNamedStream(TcpJsonNamedStream tcpJsonNamedStream)
        {
            var buffer = tcpJsonNamedStream.GetWriteCacheAndClear();
            if(buffer.Length > 0)
            {
                var package = new TcpJsonPackage
                {
                    Type = TcpJsonPackageType.NamedStream,
                    DataType = tcpJsonNamedStream.Name,
                    DataBytes = buffer
                };
                SendPackage(package);
            }
        }

        /// <summary>
        /// 关联的 <c>TcpClient</c> 对象
        /// </summary>
        public TcpClient Client { get; }

        /// <summary>
        /// 自动 Ping 的时间间隔
        /// </summary>
        public TimeSpan PingTimeSpan { get; set; } = TimeSpan.FromSeconds(5);

        /// <summary>
        /// 相关变量池
        /// </summary>
        public NameValueCollection Session { get; } = new NameValueCollection();

        /// <summary>
        /// 内部线程运行代码
        /// </summary>
        /// <param name="token">当用户调用 Stop 方法时触发取消通知</param>
        protected override void DoRun(CancellationToken token)
        {
            using (var stream = Client.GetStream())
            {
                while (!token.IsCancellationRequested)
                {
                    var wait = true;
                    if (stream.DataAvailable)
                    {
                        ReadData(stream);
                        wait = false;
                    }
                    lock (sendPackageQueue)
                    {
                        while (sendPackageQueue.Count > 0)
                        {
                            SendPackageInner(stream, sendPackageQueue.Dequeue());
                            wait = false;
                        }
                    }
                    if (wait)
                    {
                        Thread.Sleep(50);
                    }
                    if(DateTime.Now - mRemoteActiveTime > PingTimeSpan)
                    {
                        SendPackageInner(stream, new TcpJsonPackage { Type = TcpJsonPackageType.Ping });
                        mRemoteActiveTime = DateTime.Now;
                    }
                }
            }
        }

        private void ReadData(NetworkStream stream)
        {
            var size = Client.Available;
            var buffer = new byte[size];
            var readed = stream.Read(buffer, 0, size);
            readCache.Write(buffer, 0, readed);
            TryReadPackages();
        }

        private void TryReadPackages()
        {
        TryNext:
            if (readCache.Length < 5)
            {
                return;
            }
            var buffer = readCache.ToArray();
            var type = (TcpJsonPackageType)buffer[0];
            var size = BitConverter.ToInt32(buffer, 1);
            if (buffer.Length >= size + 5)
            {
                var bodyBuffer = new byte[size];
                Array.Copy(buffer, 5, bodyBuffer, 0, size);
                using (var reader = new BinaryReader(new MemoryStream(bodyBuffer)))
                {
                    var package = new TcpJsonPackage();
                    package.Type = type;
                    package.DataType = reader.ReadString();
                    if (package.Type == TcpJsonPackageType.NamedStream)
                    {
                        var byteSize = reader.ReadInt32();
                        package.DataBytes = reader.ReadBytes(byteSize);
                    }
                    else
                    {
                        package.Data = reader.ReadString();
                    }
                    Recive(package);
                }
                readCache = new MemoryStream();
                if(buffer.Length > size + 5)
                {
                    readCache.Write(buffer, size + 5, buffer.Length - size - 5);
                    readCache.Position = 0;
                }
                goto TryNext;
            }
        }

        private void Recive(TcpJsonPackage package)
        {
            switch (package.Type)
            {
                case TcpJsonPackageType.Command:
                    DoReceiveCommand(package.Data);
                    break;
                case TcpJsonPackageType.Json:
                    DoReceiveJson(package);
                    break;
                case TcpJsonPackageType.Ping:
                    DoPing();
                    break;
                case TcpJsonPackageType.NamedStream:
                    DoReceiveNamedStream(package);
                    break;
                default:
                    break;
            }
        }

        private void DoReceiveNamedStream(TcpJsonPackage package)
        {
            var namedStream = GetNamedStream(package.DataType);
            namedStream.OnDataReceive(package.DataBytes);
        }

        private void DoPing()
        {
            mRemoteActiveTime = DateTime.Now;
        }

        private void DoReceiveJson(TcpJsonPackage package)
        {
            var dataType = Type.GetType(package.DataType);
            lock (mJsonHandlers)
            {
                foreach (var handler in mJsonHandlers)
                {
                    if (handler.Type == dataType)
                    {
                        handler.Callback.DynamicInvoke(JsonConvert.DeserializeObject(package.Data, dataType), this);
                    }
                }
            }
        }

        private void DoReceiveCommand(string cmd)
        {
            lock (mCommandHandlers)
            {
                foreach (var handler in mCommandHandlers)
                {
                    handler.Invoke(cmd);
                }
            }
        }

        private void SendPackageInner(NetworkStream stream, TcpJsonPackage tcpJsonPackage)
        {
            var buffer = tcpJsonPackage.ToBytes();
            stream.Write(buffer, 0, buffer.Length);
            stream.Flush();
            tcpJsonPackage.Callback?.Invoke();
        }

        private void SendPackage(TcpJsonPackage package)
        {
            sendPackageQueue.Enqueue(package);
        }

        /// <summary>
        /// 发送一个命令
        /// </summary>
        /// <param name="command">命令</param>
        /// <param name="callback">发送后回调</param>
        public void SendCommand(string command, Action callback = null)
        {
            SendPackage(new TcpJsonPackage
            {
                Type = TcpJsonPackageType.Command,
                Data = command,
                Callback = callback
            });
        }

        /// <summary>
        /// 发送一个对象
        /// </summary>
        /// <typeparam name="T">对象类型</typeparam>
        /// <param name="obj">对象</param>
        /// <param name="callback">发送后回调</param>
        public void SendObject<T>(T obj, Action callback = null)
        {
            SendPackage(new TcpJsonPackage
            {
                Type = TcpJsonPackageType.Json,
                DataType = GetDataType(typeof(T)),
                Data = JsonConvert.SerializeObject(obj),
                Callback = callback
            });
        }

        private string GetDataType(Type objType)
        {
            return objType.AssemblyQualifiedName;
        }

        /// <summary>
        /// 触发 Stoped 事件
        /// </summary>
        protected override void OnStop()
        {
            lock (mStopHandlers)
            {
                foreach(var handler in mStopHandlers)
                {
                    handler.Invoke(this);
                }
            }
            base.OnStop();
        }

        /// <summary>
        /// 注册当收到命令时回调
        /// </summary>
        /// <param name="callback">回调方法</param>
        /// <returns></returns>
        public TcpJsonClient OnReceiveCommand(Action<string> callback)
        {
            lock (mCommandHandlers)
            {
                mCommandHandlers.Add(callback);
            }
            return this;
        }

        /// <summary>
        /// 注册当收到特定类型对象时回调
        /// </summary>
        /// <typeparam name="T">收到的对象类型</typeparam>
        /// <param name="callback">回调方法</param>
        /// <returns></returns>
        public TcpJsonClient OnReceive<T>(Action<T, TcpJsonClient> callback)
        {
            lock (mJsonHandlers)
            {
                mJsonHandlers.Add(new JsonCallback { Type = typeof(T), Callback = callback });
            }
            return this;
        }

        /// <summary>
        /// 当停止时回调
        /// </summary>
        /// <param name="callback">回调方法</param>
        /// <returns></returns>
        public TcpJsonClient OnStoped(Action<TcpJsonClient> callback)
        {
            lock (mStopHandlers)
            {
                mStopHandlers.Add(callback);
            }
            return this;
        }

        class JsonCallback
        {
            public Type Type { get; set; }
            public Delegate Callback { get; set; }
        }
    }
}
