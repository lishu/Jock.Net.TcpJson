using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace Jock.Net.TcpJson
{
    /// <summary>
    /// Provides an internal running thread management object
    /// </summary>
    public abstract class SafeThreadObject
    {
        private Thread taskThread;
        private CancellationTokenSource mCancelSource;

        internal SafeThreadObject() { }

        /// <summary>
        /// Whether the internal thread is running
        /// </summary>
        public bool IsRunning => taskThread?.ThreadState == ThreadState.Running;

        /// <summary>
        /// Start the service thread
        /// </summary>
        public void Start()
        {
            taskThread = new Thread(Run);
            taskThread.Start();
        }

        private void Run()
        {
            mCancelSource = new CancellationTokenSource();
            try
            {
                DoRun(mCancelSource.Token);
            }
            catch(Exception e)
            {
                UnhandledException?.Invoke(this, new UnhandledExceptionEventArgs(e, false));
            }
            finally
            {
                OnStop();
            }
        }

        /// <summary>
        /// Calling the <c>Stoped</c> event
        /// </summary>
        protected virtual void OnStop()
        {
            Stoped?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Triggered when an internal thread is stopped
        /// </summary>
        public event EventHandler Stoped;

        /// <summary>
        /// Occurs when an unhandled exception occurs on an internal thread
        /// </summary>
        public event UnhandledExceptionEventHandler UnhandledException;

        /// <summary>
        /// Triggered when an unhandled exception occurs on an internal thread
        /// </summary>
        /// <param name="token">Triggering a cancellation notification when the user calls the Stop method</param>
        protected abstract void DoRun(CancellationToken token);

        /// <summary>
        /// Notifies the internal thread to stop running
        /// <para>
        /// The internal execution thread must handle the <c>CancellationToken</c> to respond to the stop state.
        /// </para>
        /// </summary>
        public void Stop()
        {
            if(mCancelSource != null && !mCancelSource.IsCancellationRequested)
            {
                mCancelSource.Cancel();
            }
        }
    }
}
