using BotTidus.Configurations;
using BotTidus.Services.HealthCheck;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.ObjectPool;
using Microsoft.Extensions.Options;
using System.Reflection;
using Traq;

namespace BotTidus.Services
{
    internal class InitialAndFinalNotifierService(
        TraqApiClient traq,
        ILogger<InitialAndFinalNotifierService> logger,
        IOptions<HealthCheckAlertOptions> alertOptions,
        IOptions<TraqBotOptions> botOptions,
        ObjectPool<Traq.Models.PostMessageRequest> postMessageRequestPool,
        IHostEnvironment hostEnv
        ) : IHostedService
    {
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            if (alertOptions.Value.AlertChannelId == Guid.Empty)
            {
                logger.LogWarning("Alert channel ID is not set. Skipping initial notification.");
                return;
            }

            try
            {
                var req = postMessageRequestPool.Get();
                req.Content = $"""
                ### :white_check_mark: @{botOptions.Value.Name} started.

                | Name        | Value |
                | :---------- | :---- |
                | Environment | `{hostEnv.EnvironmentName}` |
                | Version     | `{Assembly.GetEntryAssembly()?.GetName()}` |
                """;
                req.Embed = true;
                await traq.Channels[alertOptions.Value.AlertChannelId].Messages.PostAsync(req, cancellationToken: cancellationToken);
                postMessageRequestPool.Return(req);
            }
            catch (Exception e)
            {
                logger.LogError(e, "Failed to post initial notification.");
            }
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            if (alertOptions.Value.AlertChannelId == Guid.Empty)
            {
                logger.LogWarning("Alert channel ID is not set. Skipping final notification.");
                return;
            }

            try
            {
                var req = postMessageRequestPool.Get();
                req.Content = $"""
                    ### :upside_down: @{botOptions.Value.Name} is shutting down.

                    | Name        | Value |
                    | :---------- | :---- |
                    | Environment | `{hostEnv.EnvironmentName}` |
                    | Version     | `{Assembly.GetEntryAssembly()?.GetName()}` |
                    """;
                req.Embed = true;
                await traq.Channels[alertOptions.Value.AlertChannelId].Messages.PostAsync(req, cancellationToken: cancellationToken);
                postMessageRequestPool.Return(req);
            }
            catch (Exception e)
            {
                logger.LogError(e, "Failed to post final notification.");
            }
        }
    }
}
