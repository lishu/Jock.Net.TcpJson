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
    /// Json Communication Client
    /// </summary>
    public class TcpJsonClient : SafeThreadObject
    {
        private Queue<TcpJsonPackage> sendPackageQueue = new Queue<TcpJsonPackage>();
        private MemoryStream readCache = new MemoryStream();
        private DateTime mRemoteActiveTime;
        private List<Action<string>> mCommandHandlers = new List<Action<string>>();
        private List<Action<byte[], TcpJsonClient>> mBytesHandlers = new List<Action<byte[], TcpJsonClient>>();
        private List<JsonCallback> mJsonHandlers = new List<JsonCallback>();
        private List<Action<TcpJsonClient>> mStopHandlers = new List<Action<TcpJsonClient>>();
        private List<TcpJsonNamedStream> mNamedStreams = new List<TcpJsonNamedStream>();

        /// <summary>
        /// Connect to the specified service side
        /// </summary>
        /// <param name="remoteEP">The service endpoint to connect to</param>
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
        /// Gets a named Stream object
        /// </summary>
        /// <param name="name">Stream name</param>
        /// <returns>Named stream</returns>
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
        /// The associated <c>TcpClient</c> object
        /// </summary>
        public TcpClient Client { get; }

        /// <summary>
        /// Interval for automatic Ping
        /// </summary>
        public TimeSpan PingTimeSpan { get; set; } = TimeSpan.FromSeconds(5);

        /// <summary>
        /// Correlation variable Pool
        /// </summary>
        public NameValueCollection Session { get; } = new NameValueCollection();

        /// <summary>
        /// Internal thread Run code
        /// </summary>
        /// <param name="token">Triggering a cancellation notification when the user calls the Stop method</param>
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
                    if (package.Type == TcpJsonPackageType.NamedStream || package.Type == TcpJsonPackageType.Bytes)
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
                case TcpJsonPackageType.Bytes:
                    DoReceiveBytes(package);
                    break;
                default:
                    break;
            }
        }

        private void DoReceiveBytes(TcpJsonPackage package)
        {
            mBytesHandlers.ForEach(callback => callback(package.DataBytes, this));
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
        /// Send a command
        /// </summary>
        /// <param name="command">Command</param>
        /// <param name="callback">Post-Send callback</param>
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
        /// Send an Object
        /// </summary>
        /// <typeparam name="T">Object type</typeparam>
        /// <param name="obj">Object</param>
        /// <param name="callback">Post-Send callback</param>
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

        /// <summary>
        /// Send a bytes block
        /// </summary>
        /// <param name="bytes">data to send</param>
        /// <param name="callback">Post-Send callback</param>
        /// <exception cref="ArgumentNullException">
        /// <c>bytes</c> is null
        /// </exception>
        public void SendBytes(byte[] bytes, Action callback = null)
        {
            if(bytes == null)
            {
                throw new ArgumentNullException(nameof(bytes));
            }
            SendPackage(new TcpJsonPackage
            {
                Type = TcpJsonPackageType.Bytes,
                DataBytes = bytes,
                Callback = callback
            });
        }

        private string GetDataType(Type objType)
        {
            return objType.AssemblyQualifiedName;
        }

        /// <summary>
        /// Triggering stoped Events
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
        /// Register callback when command is received
        /// </summary>
        /// <param name="callback">callback method</param>
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
        /// Register callback when a specific type of object is received
        /// </summary>
        /// <typeparam name="T">Type of object received</typeparam>
        /// <param name="callback">callback method</param>
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
        /// Register callback when a bytes block is received
        /// </summary>
        /// <param name="callback"></param>
        /// <returns></returns>
        public TcpJsonClient OnReceiveBytes(Action<byte[], TcpJsonClient> callback)
        {
            lock(mBytesHandlers)
            {
                mBytesHandlers.Add(callback);
            }
            return this;
        }

        /// <summary>
        /// Callback when stopped
        /// </summary>
        /// <param name="callback">callback method</param>
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
