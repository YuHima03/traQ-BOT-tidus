namespace BotTidus.Domain.MessageFaceScores
{
    public interface IMessageFaceScoresRepository
    {
        public ValueTask AddMessageFaceScoreAsync(MessageFaceScore score, CancellationToken ct);
        public ValueTask<MessageFaceScore> GetMessageFaceScoreAsync(Guid id, CancellationToken ct);
        public ValueTask<MessageFaceScore[]> GetMessageFaceScoresByUserIdAsync(Guid userId, CancellationToken ct);
        public ValueTask<KeyValuePair<Guid, int>[]> GetUserFaceCountsAsync(CancellationToken ct);
        public ValueTask UpdateMessageFaceScoreAsync(MessageFaceScore score, CancellationToken ct);
    }
}
