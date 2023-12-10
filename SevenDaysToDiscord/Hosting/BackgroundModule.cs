using System;
using System.Threading;
using System.Threading.Tasks;

namespace SevenDaysToDiscord.Hosting
{
    /// <summary>
    /// Base class for implemenmting a long running <see cref="IModule"/>
    /// 
    /// This is mostly copied from Microsoft.Extensions.Hosting.Abstractions
    /// https://github.com/dotnet/runtime/blob/163bc5800b469bcb86d59b77da21965916a0f4ce/src/libraries/Microsoft.Extensions.Hosting.Abstractions/src/BackgroundService.cs
    /// </summary>
    internal abstract class BackgroundModule : IModule, IDisposable
    {
        private Task _executeTask;
        private CancellationTokenSource _stoppingCts;

        public virtual Task ExecuteTask => _executeTask;

        protected abstract Task ExecuteAsync(CancellationToken cancellationToken);

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _stoppingCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            _executeTask = ExecuteAsync(_stoppingCts.Token);

            if(_executeTask.IsCompleted)
                return _executeTask;

            return Task.CompletedTask;
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            if (_executeTask == null)
                return;

            try
            {
                _stoppingCts?.Cancel();
            }
            finally
            {
                var tcs = new TaskCompletionSource<object>();
                var registration = cancellationToken.Register(s => ((TaskCompletionSource<object>)s).SetCanceled(), tcs);

                using (registration)
                {
                    await Task.WhenAny(_executeTask, tcs.Task).ConfigureAwait(false);
                }
            }
        }

        public virtual void Dispose()
        {
            _stoppingCts?.Cancel();
        }
    }
}
