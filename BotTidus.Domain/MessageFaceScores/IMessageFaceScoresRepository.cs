namespace BotTidus.Domain.MessageFaceScores
{
    public interface IMessageFaceScoresRepository
    {
        public ValueTask AddMessageFaceScore(MessageFaceScore score, CancellationToken ct);
        public ValueTask<MessageFaceScore> GetMessageFaceScoreAsync(Guid id, CancellationToken ct);
        public ValueTask<MessageFaceScore[]> GetMessageFaceScoresByUserIdAsync(Guid userId, CancellationToken ct);
        public ValueTask<KeyValuePair<Guid, int>[]> GetUserFaceCounts(CancellationToken ct);
        public ValueTask UpdateMessageFaceScore(MessageFaceScore score, CancellationToken ct);
    }
}
