namespace BotTidus.Domain.MessageFaceScores;

public interface IMessageFaceScoresRepository
{
    ValueTask AddMessageFaceScoreAsync(MessageFaceScore score, CancellationToken ct);
    ValueTask DeleteMessageFaceScoreAsync(Guid messageId, CancellationToken ct);
    ValueTask<MessageFaceScore> GetMessageFaceScoreAsync(Guid id, CancellationToken ct);
    ValueTask<MessageFaceScore?> GetMessageFaceScoreOrDefaultAsync(Guid id, CancellationToken ct);
    ValueTask<MessageFaceScore[]> GetMessageFaceScoresByUserIdAsync(Guid userId, CancellationToken ct);
    ValueTask<UserFaceCount> GetUserFaceCountAsync(Guid userId, CancellationToken ct);
    ValueTask<UserFaceCount[]> GetUserFaceCountsAsync(CancellationToken ct);
    ValueTask<MessageFaceScore> AddOrUpdateMessageFaceScoreAsync(Guid messageId, Func<MessageFaceScore?, CancellationToken, ValueTask<MessageFaceScore>> configureAsync, CancellationToken ct);
}
