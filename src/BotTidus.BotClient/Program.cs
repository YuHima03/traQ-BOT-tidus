using BotTidus.BotClient.BotServices;
using BotTidus.TraqClient;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BotTidus.BotClient
{
    internal class Program
    {
        static readonly Uri BaseUri = new("https://q.trap.jp/");

        static async Task Main(string[] args)
        {
            var accessToken = Environment.GetEnvironmentVariable("BOT_ACCESS_TOKEN");
            if (accessToken is null)
            {
                throw new Exception("Environment variable `BOT_ACCESS_TOKEN` does not exist.");
            }

            var botAppBuilder = Host.CreateApplicationBuilder();

            botAppBuilder.Services
                .AddHttpClient()
                .AddLogging(b => b.AddConsole())
                .AddTraqClient<ClientTraqService>(b => b
                    .SetBaseUri(BaseUri)
                    .UseBearerAuthorization(accessToken)
                )
                .AddHostedService<InteractiveBotService>()
                .AddHostedService<IntervalBotService>();

            var botApp = botAppBuilder.Build();
            await botApp.RunAsync();
        }
    }
}
