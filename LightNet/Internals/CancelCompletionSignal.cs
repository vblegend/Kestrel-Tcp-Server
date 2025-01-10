using System;
using System.Threading;
using System.Threading.Tasks;

namespace LightNet.Internals
{
    /// <summary>
    /// 
    /// </summary>
    internal class CancelCompletionSignal
    {
        private TaskCompletionSource stopCompleted;
        private CancellationTokenSource cancelTokenSource;
        public CancellationToken Token => cancelTokenSource.Token;
        public Boolean IsCancellationRequested => cancelTokenSource.IsCancellationRequested;
        public Boolean IsComplete {  get; private set; }

        public CancelCompletionSignal(Boolean defaultState = false)
        {
            stopCompleted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            cancelTokenSource = new CancellationTokenSource();
            if (defaultState)
            {
                stopCompleted.SetResult();
                cancelTokenSource.Cancel();
                IsComplete = true;
            }
        }

        /// <summary>
        /// 重置取消状态为未取消，未完成
        /// </summary>
        public void Reset()
        {
            IsComplete = false;
            stopCompleted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            cancelTokenSource = new CancellationTokenSource();
        }


        /// <summary>
        /// 取消完成
        /// </summary>
        public void Complete()
        {
            IsComplete = true;
            stopCompleted.SetResult();
        }


        /// <summary>
        /// 取消操作 使Token.IsCancellationRequested=true,并等待执行Complete()
        /// </summary>
        /// <returns></returns>
        public async Task CancelAsync()
        {
            cancelTokenSource.Cancel();
            await stopCompleted.Task;
        }


        /// <summary>
        /// 取消操作使Token.IsCancellationRequested=true,并 不等待。
        /// </summary>
        /// <returns></returns>
        public void Cancel()
        {
            cancelTokenSource.Cancel();
        }


        public async Task WaitAsync()
        {
            await stopCompleted.Task;
        }

    }
}
