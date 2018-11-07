#if NET35
namespace Jock.Net.TcpJson
{
    /// <summary>
    /// CancellationToken
    /// </summary>
    public class CancellationToken
    {
        /// <summary>
        /// IsCancellationRequested
        /// </summary>
        public bool IsCancellationRequested { get; internal set; }
    }

    /// <summary>
    /// CancellationToken Source
    /// </summary>
    public class CancellationTokenSource
    {
        /// <summary>
        /// CancellationToken
        /// </summary>
        public CancellationToken Token { get; } = new CancellationToken();

        /// <summary>
        /// IsCancellationRequested
        /// </summary>
        public bool IsCancellationRequested => Token.IsCancellationRequested;

        /// <summary>
        /// Set source require cancel current async op
        /// </summary>
        public void Cancel()
        {
            lock (this)
            {
                Token.IsCancellationRequested = true;
            }
        }
    }
}
#endif
