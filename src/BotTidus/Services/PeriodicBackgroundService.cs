using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace BotTidus.Services
{
    abstract class PeriodicBackgroundService(
        IOptions<PeriodicBackgroundServiceOptions> options
        ) : BackgroundService
    {
        readonly PeriodicBackgroundServiceOptions _options = options.Value;

        protected sealed override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                await Task.Delay(_options.Delay, stoppingToken);
                using PeriodicTimer timer = new(_options.Period);
                do
                {
                    await ExecuteCoreAsync(stoppingToken);
                }
                while (await timer.WaitForNextTickAsync(stoppingToken));
            }
            catch (OperationCanceledException)
            {
                return;
            }
        }

        protected abstract ValueTask ExecuteCoreAsync(CancellationToken ct);
    }

    interface IPeriodicBackgroundServiceOptions
    {
        TimeSpan Delay { get; }
        TimeSpan Period { get; }
    }

    class PeriodicBackgroundServiceOptions : IPeriodicBackgroundServiceOptions
    {
        public TimeSpan Delay { get; set; }
        public TimeSpan Period { get; set; }
    }
}
