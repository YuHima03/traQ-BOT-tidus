using BotTidus.Domain;
using BotTidus.Helpers;
using BotTidus.Services.ExternalServiceHealthCheck;
using BotTidus.Services.FaceCollector;
using BotTidus.Services.FaceReactionCollector;
using BotTidus.Services.HealthCheck;
using BotTidus.Services.InteractiveBot;
using BotTidus.Services.StampRanking;
using BotTidus.Services.WakaruMessageRanking;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.ObjectPool;
using MySql.Data.MySqlClient;
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
                        .AddTypedHostedService<TraqHealthCheckService>()
                        .AddTypedHostedService<WakaruMessageRankingService>();
                    services.Configure<HealthCheckPublisherOptions>(ctx.Configuration.GetSection(Constants.ConfigSections.HealthCheckPublisherOptionsSection));
                    services.AddSingleton<HealthCheckPublisher>().AddSingleton<IHealthCheckPublisher, HealthCheckPublisher>(static sp => sp.GetRequiredService<HealthCheckPublisher>());
                    services.AddSingleton<TraqHealthCheckPublisher>();

                    services.AddDbContextFactory<RepositoryImpl.Repository>(ob =>
                    {
                        ob.UseMySQL(GetConnectionString(ctx));
                        if (ctx.HostingEnvironment.IsDevelopment())
                        {
                            ob.EnableSensitiveDataLogging();
                        }
                    });
                    services.AddSingleton<IRepositoryFactory, RepositoryImpl.RepositoryFactory>(sp => new(sp.GetRequiredService<IDbContextFactory<RepositoryImpl.Repository>>()));

                    services.AddTraqApiClient(o =>
                    {
                        o.BaseAddress = ctx.Configuration["TRAQ_API_BASE_ADDRESS"] ?? "https://q.trap.jp/api/v3/";
                        o.BearerAuthToken = ctx.Configuration["BOT_ACCESS_TOKEN"];
                    });

                    services.AddSingleton(TimeZoneInfo.FindSystemTimeZoneById(ctx.Configuration[Constants.ConfigSections.DefaultTimeZoneSection] ?? TimeZoneInfo.Utc.Id));

                    services.AddMemoryCache(ctx.Configuration.GetSection(Constants.ConfigSections.MemoryCacheOptionsSection).Bind);

                    services.Configure<AppConfig>(conf =>
                    {
                        ctx.Configuration.Bind(conf);

                        if (string.IsNullOrWhiteSpace(conf.BotName))
                        {
                            throw new Exception("Bot name must be set and be non-empty");
                        }
                        if (string.IsNullOrWhiteSpace(conf.BotCommandPrefix))
                        {
                            throw new Exception("Command prefix must be set and be non-empty.");
                        }
                    });

                    services.AddHostedService<FaceCollectingService>();
                    services.AddHostedService<FaceReactionCollectingService>();
                    services.AddHostedService<InteractiveBotService>();
                    services.AddHostedService<StampRankingService>();
                    services.AddHostedService<TraqHealthCheckService>();
                    services.AddHostedService<WakaruMessageRankingService>();
                })
                .Build();

            using CancellationTokenSource cts = new();
            await host.RunAsync(cts.Token);
        }

        private static string GetConnectionString(HostBuilderContext ctx)
        {
            var onDocker = ctx.Configuration["ON_DOCKER"] == "true";

            MySqlConnectionStringBuilder csb = new()
            {
                UserID = ctx.Configuration["NS_MARIADB_USER"],
                Password = ctx.Configuration["NS_MARIADB_PASSWORD"],
                Database = ctx.Configuration["NS_MARIADB_DATABASE"],
            };

            if (onDocker)
            {
                csb.Server = ctx.Configuration["NS_MARIADB_HOSTNAME"];
                if (uint.TryParse(ctx.Configuration["NS_MARIADB_PORT"], out var _port))
                {
                    csb.Port = _port;
                }
            }
            else
            {
                csb.Server = ctx.Configuration["MARIADB_EXPOSE_HOSTNAME"] ?? ctx.Configuration["NS_MARIADB_HOSTNAME"];
                if (uint.TryParse(ctx.Configuration["MARIADB_EXPOSE_PORT"], out var _port) || uint.TryParse(ctx.Configuration["NS_MARIADB_PORT"], out _port))
                {
                    csb.Port = _port;
                }
            }

            return csb.ConnectionString;
        }
    }
}
