using System.Threading;
using System.Threading.Tasks;

namespace SevenDaysToDiscord.Hosting
{
    internal interface IModule
    {
        Task StartAsync(CancellationToken cancellationToken);
        Task StopAsync(CancellationToken cancellationToken);
    }
}
