using BotTidus.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
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
                    services.AddLogging(b => b
                        .AddSimpleConsole(o =>
                        {
                            o.ColorBehavior = Microsoft.Extensions.Logging.Console.LoggerColorBehavior.Enabled;
                            o.IncludeScopes = true;
                        })
                        .SetMinimumLevel(ctx.HostingEnvironment.IsDevelopment() ? LogLevel.Debug : LogLevel.Information)
                    );

                    services.AddDbContextFactory<RepositoryImpl.Repository>(ob =>
                    {
                        ob.UseMySQL(GetConnectionString(ctx));
                    });
                    services.AddTransient<IRepositoryFactory, RepositoryImpl.RepositoryFactory>(sp => new(sp.GetRequiredService<IDbContextFactory<RepositoryImpl.Repository>>()));

                    services.AddTraqApiClient(o =>
                    {
                        o.BaseAddress = ctx.Configuration["TRAQ_API_BASE_ADDRESS"] ?? "https://q.trap.jp/api/v3/";
                        o.BearerAuthToken = ctx.Configuration["BOT_ACCESS_TOKEN"];
                    });

                    services.AddSingleton(TimeZoneInfo.FindSystemTimeZoneById("Tokyo Standard Time"));

                    services.AddMemoryCache(o =>
                    {
                        o.ExpirationScanFrequency = TimeSpan.FromMinutes(1);
                    });

                    services.Configure<AppConfig>(conf =>
                    {
                        var botName = ctx.Configuration["BOT_NAME"];
                        if (string.IsNullOrEmpty(botName))
                        {
                            throw new Exception("The value of BOT_NAME must be set and be not empty.");
                        }
                        conf.BotName = botName;

                        if (Guid.TryParse(ctx.Configuration["BOT_USER_ID"], out var userId))
                        {
                            conf.BotUserId = userId;
                        }

                        conf.BotCommandPrefix = ctx.HostingEnvironment.IsDevelopment() ? "_//" : "//";
                    });

                    services.AddHostedService<Services.FaceCollectingService>(p => new(p));
                    services.AddHostedService<Services.InteractiveBotService>();
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
