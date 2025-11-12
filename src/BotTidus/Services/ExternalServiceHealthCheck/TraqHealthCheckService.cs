using System.Net;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Kiota.Abstractions;
using Traq;

namespace BotTidus.Services.ExternalServiceHealthCheck;

class TraqHealthCheckService(
    ILogger<TraqHealthCheckService> logger,
    TraqApiClient traq,
    TraqHealthCheckPublisher publisher) : BackgroundService, IHealthCheck
{
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
                _ = await traq.Users.Me.GetAsync(cancellationToken: stoppingToken) ?? throw new Exception("Health check failed: traQ API '/users/me' response was null.");
                publisher.CurrentStatus = TraqStatus.Available;
            }
            catch (ApiException ex)
            {
                switch (ex.ResponseStatusCode)
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
            catch (Exception ex)
            {
                publisher.CurrentStatus = TraqStatus.Unknown;
                logger.LogError(ex, "Failed to check traQ health.");
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
