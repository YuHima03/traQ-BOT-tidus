using BotTidus.TraqBot;
using BotTidus.TraqClient;
using BotTidus.TraqClient.Models;
using Microsoft.Extensions.DependencyInjection;

namespace BotTidus.BotClient.BotServices
{
    internal class InteractiveBotService(IServiceProvider provider) : TraqWebSocketBotService(provider)
    {
        readonly Traq _traq = provider.GetRequiredService<IClientTraqService>().Traq;

        protected override ValueTask OnMessageCreated(CancellationToken ct)
        {
            return base.OnMessageCreated(ct);
        }
    }
}
