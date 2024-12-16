using BotTidus.TraqClient;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BotTidus.TraqBot
{
    public class TraqBotService(IServiceProvider services) : BackgroundService
    {
        readonly ILogger<TraqBotService> _logger = services.GetRequiredService<ILogger<TraqBotService>>();

        readonly IClientTraqService _traq = services.GetRequiredService<IClientTraqService>();

        protected override Task ExecuteAsync(CancellationToken ct)
        {
            return Task.CompletedTask;
        }

        public override Task StartAsync(CancellationToken ct)
        {
            _logger.LogInformation("Starting a traQ bot.");
            return base.StartAsync(ct);
        }

        public override Task StopAsync(CancellationToken ct)
        {
            _logger.LogInformation("Stopping the traQ bot.");
            return base.StopAsync(ct);
        }
    }
}
