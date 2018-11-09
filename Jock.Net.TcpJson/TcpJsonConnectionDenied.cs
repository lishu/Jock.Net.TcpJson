using System;
using System.Collections.Generic;
using System.Text;

namespace Jock.Net.TcpJson
{
    /// <summary>
    /// EventArgs for TcpJsonClient connection denied
    /// </summary>
    public class TcpJsonConnectionDeniedEventArgs : EventArgs
    {
        internal TcpJsonConnectionDeniedEventArgs(TcpJsonClient client)
        {
            this.Client = client;
        }

        /// <summary>
        /// TcpJsonClient instance
        /// </summary>
        public TcpJsonClient Client { get; }

        /// <summary>
        /// set True if want try reconnection
        /// </summary>
        public bool Retry { get; set; }
    }

    /// <summary>
    /// Delegate for TcpJsonClient connection denied
    /// </summary>
    /// <param name="e"></param>
    public delegate void TcpJsonConnectionDeniedEventHandler(TcpJsonConnectionDeniedEventArgs e);
}
