namespace BotTidus.Domain
{
    public interface IRepository :
        IDisposable,
        IAsyncDisposable,
        DiscordWebhook.IDiscordWebhooksRepository,
        MessageFaceScores.IMessageFaceScoresRepository;
}
