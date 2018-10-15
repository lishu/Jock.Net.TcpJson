using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Jock.Net.TcpJson
{
    /// <summary>
    /// 提供一个与 <c>TcpJsonClient</c> 关联的命名流，它复用现有网络流通道
    /// </summary>
    public class TcpJsonNamedStream : Stream
    {
        private const long FREE_READED_CACHE_SIZE = 1024 * 1024;
        private TcpJsonClient tcpJsonClient;
        private MemoryStream readCache = new MemoryStream();
        private MemoryStream writeCache = new MemoryStream();

        internal TcpJsonNamedStream(TcpJsonClient tcpJsonClient)
        {
            this.tcpJsonClient = tcpJsonClient;
        }

        internal void ClearReadCacheIfNeed()
        {
            if (readCache.Position > FREE_READED_CACHE_SIZE)
            {
                lock (readCache)
                {
                    var len = readCache.Length;
                    var pos = readCache.Position;
                    var buffer = readCache.ToArray();
                    readCache.SetLength(len - pos);
                    readCache.Position = 0;
                    readCache.Write(buffer, (int)pos, (int)readCache.Length);
                    readCache.Position = 0;
                }
            }
        }

        /// <summary>
        /// 流名称
        /// </summary>
        public string Name { get; internal set; }

        /// <summary>
        /// 流是否可读，总是返回 true
        /// </summary>
        public override bool CanRead => true;

        /// <summary>
        /// 流是否可非顺序访问，总是返回 false
        /// </summary>
        public override bool CanSeek => false;

        /// <summary>
        /// 流是否可写，总是返回 false
        /// </summary>
        public override bool CanWrite => true;

        /// <summary>
        /// 是否在任何 <code>Write</code> 方法之后自动调用 <code>Flush</code>
        /// </summary>
        public bool AutoFlush { get; set; }

        /// <summary>
        /// 获取流长度，总是触发 <c>NotSupportedException</c> 异常
        /// </summary>
        /// <exception cref="NotSupportedException" />
        public override long Length => throw new NotSupportedException();

        /// <summary>
        /// 获取或设置流访问偏移，总是触发 <c>NotSupportedException</c> 异常
        /// </summary>
        /// <exception cref="NotSupportedException" />
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

        public override void Flush()
        {
            tcpJsonClient.FlushNamedStream(this);
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            lock (readCache)
            {
                int readed = readCache.Read(buffer, offset, count);
                if(readed > 0)
                {
                    ClearReadCacheIfNeed();
                }
                return readed;
            }
        }

        [System.Diagnostics.DebuggerStepThrough]
        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        [System.Diagnostics.DebuggerStepThrough]
        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            lock (writeCache)
            {
                writeCache.Write(buffer, offset, count);
            }
            if (AutoFlush)
            {
                Flush();
            }
        }

        internal byte[] GetWriteCacheAndClear()
        {
            lock(writeCache)
            {
                var buffer = writeCache.ToArray();
                writeCache.SetLength(0);
                writeCache.Position = 0;
                return buffer;
            }
        }

        /// <summary>
        /// 获取已接受还未读取的字符数
        /// </summary>
        public int DataAvailable
        {
            get
            {
                lock (readCache)
                {
                    return (int)(readCache.Length - readCache.Position);
                }
            }
        }

        internal void OnDataReceive(byte[] dataBytes)
        {
            lock (readCache)
            {
                var readPosition = readCache.Position;
                readCache.Position = readCache.Length;
                readCache.Write(dataBytes, 0, dataBytes.Length);
                readCache.Position = readPosition;
            }
        }
    }
}
