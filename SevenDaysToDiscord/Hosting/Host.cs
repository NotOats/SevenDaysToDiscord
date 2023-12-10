using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SevenDaysToDiscord.Hosting
{
    internal class Host : IDisposable
    {
        private readonly IEnumerable<IModule> _modules;

        public Host(IEnumerable<IModule> modules)
        {
            _modules = modules ?? throw new ArgumentNullException(nameof(modules));
        }

        public void Dispose()
        {
            Stop();
        }

        public void Start()
        {
            var task = Task.Run(async () =>
            {
                await ForeachService(_modules, true,
                    (service, token) => service.StartAsync(token)).ConfigureAwait(false);
            });

            task.GetAwaiter().GetResult();
        }

        public void Stop()
        {
            var task = Task.Run(async () =>
            {
                IEnumerable<IModule> reversedModules = _modules.Reverse();

                await ForeachService(reversedModules, true,
                    (service, token) => service.StopAsync(token)).ConfigureAwait(false);
            });

            task.GetAwaiter().GetResult();
        }

        // The following method is derived from
        // https://github.com/dotnet/runtime/blob/163bc5800b469bcb86d59b77da21965916a0f4ce/src/libraries/Microsoft.Extensions.Hosting/src/Internal/Host.cs
        private static async Task ForeachService<T>(
            IEnumerable<T> services,
            bool abortOnFirstException,
            Func<T, CancellationToken, Task> operation)
        {
            var exceptions = new List<Exception>();

            foreach (T service in services)
            {
                try
                {
                    await operation(service, default(CancellationToken)).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);

                    if (abortOnFirstException)
                        return;
                }
            }

            if (exceptions.Count == 0)
                return;

            if (exceptions.Count == 1)
                ExceptionDispatchInfo.Capture(exceptions[0]).Throw();
            else
                throw new AggregateException("One or more modules failed to execute.", exceptions);
        }
    }
}
