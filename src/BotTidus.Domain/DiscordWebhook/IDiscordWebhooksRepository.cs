namespace BotTidus.Domain.DiscordWebhook;

public interface IDiscordWebhooksRepository
{
    ValueTask<DiscordWebhook[]> GetDiscordWebhooksAsync(bool includeDisabled, CancellationToken ct);
}
