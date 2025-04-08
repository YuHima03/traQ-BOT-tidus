using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.NetworkInformation;
using Traq;

namespace BotTidus.Services.ExternalServiceHealthCheck
{
    internal class TraqHealthCheckService(
        ILogger<TraqHealthCheckService> logger,
        ITraqApiClient traq,
        TraqHealthCheckPublisher publisher) : BackgroundService, IHealthCheck
    {
        readonly Ping ping = new();
        readonly string traqHostName = new Uri(traq.Options.BaseAddress).Host;

        public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            return publisher.CurrentStatus switch
            {
                TraqStatus.Available => Task.FromResult(HealthCheckResult.Healthy()),
                TraqStatus.PermissionDenied => Task.FromResult(HealthCheckResult.Degraded("The client does not have permission to access the traQ service.")),
                TraqStatus.Unavailable => Task.FromResult(HealthCheckResult.Unhealthy("The traQ service is unavailable.")),
                _ => Task.FromResult(HealthCheckResult.Unhealthy("The traQ service status is unknown.")),
            };
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using PeriodicTimer timer = new(TimeSpan.FromMinutes(1));
            do
            {
                publisher.LastCheckedAt = DateTimeOffset.UtcNow;
                try
                {
                    _ = await traq.MeApi.GetMeAsync(cancellationToken: stoppingToken);
                    publisher.CurrentStatus = TraqStatus.Available;
                }
                catch (Exception ex)
                {
                    if (ex is Traq.Client.ApiException apiException)
                    {
                        switch (apiException.ErrorCode)
                        {
                            case (int)HttpStatusCode.Forbidden:
                            case (int)HttpStatusCode.Unauthorized:
                                publisher.CurrentStatus = TraqStatus.PermissionDenied;
                                break;

                            case (int)HttpStatusCode.ServiceUnavailable:
                                publisher.CurrentStatus = TraqStatus.Unavailable;
                                break;

                            default:
                                publisher.CurrentStatus = TraqStatus.Unknown;
                                logger.LogError(ex, "Unknown response from the traQ service.");
                                break;
                        }
                    }
                    else
                    {
                        publisher.CurrentStatus = TraqStatus.Unknown;
                        logger.LogError(ex, "Failed to check traQ health.");
                    }
                }
            }
            while (await timer.WaitForNextTickAsync(stoppingToken));
        }
    }

    enum TraqStatus : byte
    {
        Unknown = 0,
        Unavailable = 1,
        PermissionDenied = 2,
        Available = 3,
    }
}
