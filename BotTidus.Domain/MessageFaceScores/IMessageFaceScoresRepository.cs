namespace BotTidus.Domain.MessageFaceScores
{
    public interface IMessageFaceScoresRepository
    {
        public ValueTask AddMessageFaceScoreAsync(MessageFaceScore score, CancellationToken ct);
        public ValueTask DeleteMessageFaceScoreAsync(Guid messageId, CancellationToken ct);
        public ValueTask<MessageFaceScore> GetMessageFaceScoreAsync(Guid id, CancellationToken ct);
        public ValueTask<MessageFaceScore[]> GetMessageFaceScoresByUserIdAsync(Guid userId, CancellationToken ct);
        public ValueTask<UserFaceCount> GetUserFaceCountAsync(Guid userId, CancellationToken ct);
        public ValueTask<UserFaceCount[]> GetUserFaceCountsAsync(CancellationToken ct);
        public ValueTask UpdateMessageFaceScoreAsync(MessageFaceScore score, CancellationToken ct);
    }
}
