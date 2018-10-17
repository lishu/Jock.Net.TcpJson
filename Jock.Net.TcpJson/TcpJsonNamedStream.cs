using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Jock.Net.TcpJson
{
    /// <summary>
    /// Provides a named stream associated with the <c>TcpJsonClient</c> , which re-uses the existing network stream channel
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
        /// Get named
        /// </summary>
        public string Name { get; internal set; }

        /// <summary>
        /// Gets whether the stream is readable and always returns true
        /// </summary>
        public override bool CanRead => true;

        /// <summary>
        /// Whether the stream can be accessed in a non-sequential order and always returns false
        /// </summary>
        public override bool CanSeek => false;

        /// <summary>
        /// Whether the stream is writable and always returns false
        /// </summary>
        public override bool CanWrite => true;

        /// <summary>
        /// Whether auto invoke <c>Flush()</c> after <c>Write()</c> is invoked 
        /// </summary>
        public bool AutoFlush { get; set; }

        /// <summary>
        /// Get stream length, always trigger <c>NotSupportedException</c> exception
        /// </summary>
        /// <exception cref="NotSupportedException" />
        public override long Length => throw new NotSupportedException();

        /// <summary>
        /// Gets or sets the stream access offset, which always triggers the <c>notsupportedexception</c> exception
        /// </summary>
        /// <exception cref="NotSupportedException" />
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

        /// <summary>
        /// Flush stream send all write data to remote
        /// </summary>
        public override void Flush()
        {
            tcpJsonClient.FlushNamedStream(this);
        }

        /// <summary>
        /// Reads a block of bytes from the current stream and writes the data to a buffer.
        /// </summary>
        /// <param name="buffer">
        /// When this method returns, contains the specified byte array with the values between
        /// offset and (offset + count - 1) replaced by the characters read from the current
        /// stream.
        /// </param>
        /// <param name="offset">
        /// The zero-based byte offset in buffer at which to begin storing data from the current stream.
        /// </param>
        /// <param name="count">
        /// The maximum number of bytes to read.
        /// </param>
        /// <returns>
        /// The total number of bytes written into the buffer. This can be less than the
        ///     number of bytes requested if that number of bytes are not currently available,
        ///     or zero if the end of the stream is reached before any bytes are read.</returns>
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

        /// <summary>
        /// Seek access position, Always throw <c>NotSupportedException</c>
        /// </summary>
        [System.Diagnostics.DebuggerStepThrough]
        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Seek stream length, Always throw <c>NotSupportedException</c>
        /// </summary>
        [System.Diagnostics.DebuggerStepThrough]
        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Writes a block of bytes to the current stream using data read from a buffer.
        /// </summary>
        /// <param name="buffer">The buffer to write data from.</param>
        /// <param name="offset">The zero-based byte offset in buffer at which to begin copying bytes to the current stream.</param>
        /// <param name="count">The maximum number of bytes to write.</param>
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
        /// Gets the number of characters received that have not yet been read
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
