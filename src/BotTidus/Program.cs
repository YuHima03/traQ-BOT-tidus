using BotTidus.Configurations;
using BotTidus.Domain;
using BotTidus.Helpers;
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
                    services.AddSingleton<IConfigureOptions<TraqApiClientOptions>>(sp => new ConfigureOptions<TraqApiClientOptions>(o =>
                    {
                        var botOptions = sp.GetRequiredService<IOptions<TraqBotOptions>>().Value;
                        o.BaseAddress = botOptions.TraqApiBaseAddress!;
                        o.BearerAuthToken = botOptions.TraqAccessToken;
                    }));

                    services.AddSingleton<ObjectPoolProvider, DefaultObjectPoolProvider>()
                        .AddObjectPool<Traq.Model.PostBotActionJoinRequest>()
                        .AddObjectPool<Traq.Model.PostBotActionLeaveRequest>()
                        .AddObjectPool<Traq.Model.PostMessageRequest>()
                        .AddObjectPool<Traq.Model.PostMessageStampRequest>();

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
                        ob.UseMySQL(sp.GetRequiredService<IOptions<DbConnectionOptions>>().Value.GetConnectionString());
                        if (ctx.HostingEnvironment.IsDevelopment())
                        {
                            ob.EnableSensitiveDataLogging();
                        }
                    });
                    services.AddSingleton<IRepositoryFactory, RepositoryImpl.RepositoryFactory>(sp => new(sp.GetRequiredService<IDbContextFactory<RepositoryImpl.Repository>>()));

                    services.AddSingleton<ITraqApiClient, TraqApiClient>();

                    services.AddSingleton(TimeZoneInfo.FindSystemTimeZoneById(ctx.Configuration[Constants.ConfigSections.DefaultTimeZoneSection] ?? TimeZoneInfo.Utc.Id));

                    services.AddMemoryCache(ctx.Configuration.GetSection(Constants.ConfigSections.MemoryCacheOptionsSection).Bind);

                    services.AddHostedService<FaceCollectingService>();
                    services.AddHostedService<FaceReactionCollectingService>();
                    services.AddHostedService<InteractiveBotService>();
                    services.AddHostedService<StampRankingService>();
                    services.AddHostedService<TraqHealthCheckService>();
                })
                .Build();

            using CancellationTokenSource cts = new();
            await host.RunAsync(cts.Token);
        }
    }
}
