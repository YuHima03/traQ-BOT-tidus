using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BotTidus.BotClient.BotServices
{
    internal class IntervalBotService(IServiceProvider provider) : BackgroundService
    {
        readonly ILogger<IntervalBotService>? _logger = provider.GetService<ILogger<IntervalBotService>>();

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromMinutes(2), stoppingToken);
            }
        }
    }
}
