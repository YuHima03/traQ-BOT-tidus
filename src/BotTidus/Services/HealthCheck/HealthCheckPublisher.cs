﻿using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.ObjectPool;
using Microsoft.Extensions.Options;
using System.Text;
using Traq;

namespace BotTidus.Services.HealthCheck
{
    internal sealed class HealthCheckPublisher(
        IOptions<AppConfig> config,
        ILogger<HealthCheckPublisher> logger,
        ObjectPool<Traq.Model.PostMessageRequest> postMessageRequestPool,
        ITraqApiClient traq
        ) : IHealthCheckPublisher
    {
        public HealthReport? LastReport { get; private set; }

        public async Task PublishAsync(HealthReport report, CancellationToken cancellationToken)
        {
            var conf = config.Value;
            if (conf.HealthAlertChannelId == Guid.Empty)
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
                await traq.MessageApi.PostMessageAsync(conf.HealthAlertChannelId, req, cancellationToken);
                postMessageRequestPool.Return(req);
            }
            catch (Exception e)
            {
                logger.LogError(e, "Failed to post health check alert.");
            }
        }
    }
}
