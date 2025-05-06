namespace BotTidus.Domain
{
    public interface IRepository :
        IDisposable,
        IAsyncDisposable,
        MessageFaceScores.IMessageFaceScoresRepository;
}
