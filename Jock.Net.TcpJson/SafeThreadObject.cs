using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace Jock.Net.TcpJson
{
    public abstract class SafeThreadObject
    {
        private Thread taskThread;
        private CancellationTokenSource mCancelSource;

        internal SafeThreadObject() { }

        public bool IsRunning => taskThread?.ThreadState == ThreadState.Running;

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

        protected virtual void OnStop()
        {
            Stoped?.Invoke(this, EventArgs.Empty);
        }

        public event EventHandler Stoped;

        public event UnhandledExceptionEventHandler UnhandledException;

        protected abstract void DoRun(CancellationToken token);

        public void Stop()
        {
            if(mCancelSource != null && !mCancelSource.IsCancellationRequested)
            {
                mCancelSource.Cancel();
            }
        }
    }
}
