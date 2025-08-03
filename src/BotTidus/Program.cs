using BotTidus.Configurations;
using BotTidus.Domain;
using BotTidus.Helpers;
using BotTidus.Services;
using BotTidus.Services.DiscordWebhook;
using BotTidus.Services.ExternalServiceHealthCheck;
using BotTidus.Services.FaceCollector;
using BotTidus.Services.FaceReactionCollector;
using BotTidus.Services.HealthCheck;
using BotTidus.Services.InteractiveBot;
using BotTidus.Services.StampRanking;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.ObjectPool;
using Microsoft.Extensions.Options;
using Microsoft.Kiota.Abstractions.Authentication;
using Microsoft.Kiota.Http.HttpClientLibrary;
using System.Text.Json.Serialization;
using Traq;

namespace BotTidus
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            var host = Host.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration((ctx, conf) =>
                {
                    var envFiles = ctx.Configuration["env-files"]?.Split(';') ?? [];
                    foreach (var envFile in envFiles)
                    {
                        var fs = File.OpenRead(envFile);
                        conf.AddIniStream(fs);
                    }
                })
                .ConfigureServices((ctx, services) =>
                {
                    services.Configure<DbConnectionOptions>(ctx.Configuration);
                    services.Configure<TraqBotOptions>(conf =>
                    {
                        ctx.Configuration.Bind(conf);
                        if (string.IsNullOrWhiteSpace(conf.Name))
                        {
                            throw new Exception("Bot name must be set and be non-empty");
                        }
                        if (string.IsNullOrWhiteSpace(conf.CommandPrefix))
                        {
                            throw new Exception("Command prefix must be set and be non-empty.");
                        }
                    });

                    services.AddSingleton(sp =>
                    {
                        var botOptions = sp.GetRequiredService<IOptions<TraqBotOptions>>().Value;
                        var httpClient = new HttpClient();
                        httpClient.DefaultRequestHeaders.Authorization = new("Bearer", botOptions.TraqAccessToken);
                        HttpClientRequestAdapter adopter = new(new AnonymousAuthenticationProvider(), httpClient: httpClient)
                        {
                            BaseUrl = botOptions.TraqApiBaseAddress,
                        };
                        return new TraqApiClient(adopter);
                    });

                    services.AddSingleton<ObjectPoolProvider, DefaultObjectPoolProvider>()
                        .AddObjectPool<Traq.Models.PostBotActionJoinRequest>()
                        .AddObjectPool<Traq.Models.PostBotActionLeaveRequest>()
                        .AddObjectPool<Traq.Models.PostMessageRequest>()
                        .AddObjectPool<Traq.Models.PostMessageStampRequest>();

                    services.AddHealthChecks()
                        .AddMySqlWithDbContext<RepositoryImpl.Repository>()
                        .AddTypedHostedService<FaceCollectingService>()
                        .AddTypedHostedService<FaceReactionCollectingService>()
                        .AddTypedHostedService<StampRankingService>()
                        .AddTypedHostedService<TraqHealthCheckService>();
                    services.Configure<HealthCheckPublisherOptions>(ctx.Configuration.GetSection(Constants.ConfigSections.HealthCheckPublisherOptionsSection));
                    services.Configure<HealthCheckAlertOptions>(ctx.Configuration);
                    services.AddSingleton<HealthCheckPublisher>().AddSingleton<IHealthCheckPublisher, HealthCheckPublisher>(static sp => sp.GetRequiredService<HealthCheckPublisher>());
                    services.AddSingleton<TraqHealthCheckPublisher>();

                    services.AddDbContextFactory<RepositoryImpl.Repository>((sp, ob) =>
                    {
                        var connStr = sp.GetRequiredService<IOptions<DbConnectionOptions>>().Value.GetConnectionString();
                        ob.UseMySql(connStr, MySqlServerVersion.LatestSupportedServerVersion);
                        ob.UseModel(RepositoryImpl.CompiledModels.RepositoryModel.Instance);
                        if (ctx.HostingEnvironment.IsDevelopment())
                        {
                            ob.EnableSensitiveDataLogging();
                        }
                    });
                    services.AddSingleton<IRepositoryFactory, RepositoryImpl.RepositoryFactory>(sp => new(sp.GetRequiredService<IDbContextFactory<RepositoryImpl.Repository>>()));

                    services.AddSingleton(GetDefaultTimeZoneInfo(ctx.Configuration));

                    services.Configure<Microsoft.Extensions.Caching.Memory.MemoryCacheOptions>(ctx.Configuration.GetSection(Constants.ConfigSections.MemoryCacheOptionsSection).Bind);
                    services.AddMemoryCache();

                    services.AddHostedService<FaceCollectingService>();
                    services.AddHostedService<FaceReactionCollectingService>();
                    services.AddHostedService<InteractiveBotService>();
                    services.Configure<StampRankingServiceOptions>(ctx.Configuration).AddHostedService<StampRankingService>();
                    services.AddHostedService<TraqHealthCheckService>();
                    services.AddHostedService<InitialAndFinalNotifierService>();
                    services.AddHostedService<DiscordWebhookService>();
                })
                .Build();

            using CancellationTokenSource cts = new();
            await host.RunAsync(cts.Token);
        }

        static TimeZoneInfo GetDefaultTimeZoneInfo(IConfiguration configuration)
        {
            if (OperatingSystem.IsWindows())
            {
                var id = configuration[Constants.ConfigSections.DefaultTimeZoneSection];
                return (!string.IsNullOrWhiteSpace(id) && TimeZoneInfo.TryFindSystemTimeZoneById(id, out var tzi)) ? tzi : TimeZoneInfo.Utc;
            }
            else if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS())
            {
                var id = configuration[Constants.ConfigSections.DefaultTimeZoneIanaSection];
                return (!string.IsNullOrWhiteSpace(id) && TimeZoneInfo.TryFindSystemTimeZoneById(id, out var tzi)) ? tzi : TimeZoneInfo.Utc;
            }
            return TimeZoneInfo.Utc;
        }
    }

    [JsonSerializable(typeof(DiscordWebhookMessage))]
    partial class AppJsonSerializerContext : JsonSerializerContext;
}
