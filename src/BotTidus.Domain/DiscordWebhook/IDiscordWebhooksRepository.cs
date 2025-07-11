namespace BotTidus.Domain.DiscordWebhook
{
    public interface IDiscordWebhooksRepository
    {
        public ValueTask<DiscordWebhook[]> GetDiscordWebhooksAsync(bool includeDisabled, CancellationToken ct);
    }
}
