using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace Jock.Net.TcpJson
{
    public class TcpJsonClient : SafeThreadObject
    {
        private Queue<TcpJsonPackage> sendPackageQueue = new Queue<TcpJsonPackage>();
        private MemoryStream readCache = new MemoryStream();
        private DateTime mRemoteActiveTime;
        private List<Action<string>> mCommandHandlers = new List<Action<string>>();
        private List<JsonCallback> mJsonHandlers = new List<JsonCallback>();
        private List<Action<TcpJsonClient>> mStopHandlers = new List<Action<TcpJsonClient>>();

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

        public TcpClient Client { get; }

        public TimeSpan PingTimeSpan { get; set; } = TimeSpan.FromSeconds(5);

        public NameValueCollection Session { get; } = new NameValueCollection();

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
                    package.Data = reader.ReadString();
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
            }
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

        public void SendCommand(string command, Action callback = null)
        {
            SendPackage(new TcpJsonPackage
            {
                Type = TcpJsonPackageType.Command,
                Data = command,
                Callback = callback
            });
        }

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

        public TcpJsonClient OnReceiveCommand(Action<string> callback)
        {
            lock (mCommandHandlers)
            {
                mCommandHandlers.Add(callback);
            }
            return this;
        }

        public TcpJsonClient OnReceive<T>(Action<T, TcpJsonClient> callback)
        {
            lock (mJsonHandlers)
            {
                mJsonHandlers.Add(new JsonCallback { Type = typeof(T), Callback = callback });
            }
            return this;
        }

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
