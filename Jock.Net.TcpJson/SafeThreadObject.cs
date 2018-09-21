using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace Jock.Net.TcpJson
{
    /// <summary>
    /// 提供一个内部运行线程管理对象
    /// </summary>
    public abstract class SafeThreadObject
    {
        private Thread taskThread;
        private CancellationTokenSource mCancelSource;

        internal SafeThreadObject() { }

        /// <summary>
        /// 内部线程是否正在运行
        /// </summary>
        public bool IsRunning => taskThread?.ThreadState == ThreadState.Running;

        /// <summary>
        /// 启动服务线程
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
        /// 调用 Stoped 事件
        /// </summary>
        protected virtual void OnStop()
        {
            Stoped?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// 当内部线程停止时发生
        /// </summary>
        public event EventHandler Stoped;

        /// <summary>
        /// 当内部线程发生未处理异常时发生
        /// </summary>
        public event UnhandledExceptionEventHandler UnhandledException;

        /// <summary>
        /// 内部线程运行代码
        /// </summary>
        /// <param name="token">当用户调用 Stop 方法时触发取消通知</param>
        protected abstract void DoRun(CancellationToken token);

        /// <summary>
        /// 通知内部线程停止运行
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
