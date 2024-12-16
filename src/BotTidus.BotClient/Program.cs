using BotTidus.TraqBot;
using BotTidus.TraqClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BotTidus.BotClient
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            var accessToken = Environment.GetEnvironmentVariable("BOT_ACCESS_TOKEN");
            if (accessToken is null)
            {
                throw new Exception("Environment variable `BOT_ACCESS_TOKEN` does not exist.");
            }

            var botAppBuilder = Host.CreateApplicationBuilder();

            botAppBuilder.Services
                .AddHostedService<TraqBotService>()
                .AddHttpClient()
                .AddLogging(b => b.AddConsole())
                .AddSingleton<IClientTraqService, ClientTraqService>();

            var botApp = botAppBuilder.Build();
            await botApp.RunAsync();
        }
    }
}
