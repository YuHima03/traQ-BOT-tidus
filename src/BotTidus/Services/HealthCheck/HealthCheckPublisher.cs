using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.ObjectPool;
using Microsoft.Extensions.Options;
using System.Text;
using Traq;

namespace BotTidus.Services.HealthCheck
{
    internal sealed class HealthCheckPublisher(
        IOptions<HealthCheckAlertOptions> options,
        ILogger<HealthCheckPublisher> logger,
        ObjectPool<Traq.Model.PostMessageRequest> postMessageRequestPool,
        ITraqApiClient traq
        ) : IHealthCheckPublisher
    {
        public HealthReport? LastReport { get; private set; }

        public async Task PublishAsync(HealthReport report, CancellationToken cancellationToken)
        {
            var opt = options.Value;
            if (opt.AlertChannelId == Guid.Empty)
            {
                logger.LogWarning("The channel to post alerts is not set.");
                return;
            }

            var lastEntries = LastReport?.Entries;
            var alertTargets = report.Entries.Where(kvp =>
            {
                var (name, entry) = kvp;
                if (entry.Status == HealthStatus.Healthy)
                {
                    return false;
                }
                return LastReport is null || !LastReport.Entries.TryGetValue(name, out var lastEntry) || lastEntry.Status == HealthStatus.Healthy;
            });

            if (!alertTargets.Any())
            {
                LastReport = report;
                return;
            }

            StringBuilder sb = new("""
                    ### :fire: Health Check Alert

                    | Name | Status | Description |
                    | :--- | :----: | :---------- |
                    """);
            sb.AppendLine();
            foreach (var (name, entry) in alertTargets)
            {
                var statusBadge = entry.Status switch
                {
                    HealthStatus.Degraded => ":warning:",
                    HealthStatus.Unhealthy => ":x:",
                    _ => ""
                };
                sb.AppendLine($"| `{name}` | {statusBadge} | {entry.Description} |");
            }

            try
            {
                var req = postMessageRequestPool.Get();
                req.Content = sb.ToString();
                req.Embed = false;
                await traq.MessageApi.PostMessageAsync(opt.AlertChannelId, req, cancellationToken);
                postMessageRequestPool.Return(req);
            }
            catch (Exception e)
            {
                logger.LogError(e, "Failed to post health check alert.");
            }
        }
    }

    sealed class HealthCheckAlertOptions
    {
        [ConfigurationKeyName("HEALTH_ALERT_CHANNEL_ID")]
        public Guid AlertChannelId { get; set; }
    }
}
