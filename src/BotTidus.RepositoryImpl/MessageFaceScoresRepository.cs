using BotTidus.Domain.MessageFaceScores;
using Microsoft.EntityFrameworkCore;

namespace BotTidus.RepositoryImpl
{
    public sealed partial class Repository
    {
        async ValueTask IMessageFaceScoresRepository.AddMessageFaceScoreAsync(MessageFaceScore score, CancellationToken ct)
        {
            _ = await MessageFaceScores.AddAsync(new Models.MessageFaceScore()
            {
                MessageId = score.MessageId,
                UserId = score.AuthorId,
                NegativePhraseCount = score.NegativePhraseCount,
                NegativeReactionCount = score.NegativeReactionCount,
                PositivePhraseCount = score.PositivePhraseCount,
                PositiveReactionCount = score.PositiveReactionCount
            }, ct);
            await SaveChangesAsync(ct);
            return;
        }

        async ValueTask IMessageFaceScoresRepository.DeleteMessageFaceScoreAsync(Guid id, CancellationToken ct)
        {
            var records = await MessageFaceScores.Where(r => r.MessageId == id).ToArrayAsync(ct);
            foreach (var r in records)
            {
                MessageFaceScores.Remove(r);
            }
            await SaveChangesAsync(ct);
            return;
        }

        async ValueTask<MessageFaceScore> IMessageFaceScoresRepository.GetMessageFaceScoreAsync(Guid id, CancellationToken ct)
        {
            return await MessageFaceScores
                .Where(r => r.MessageId == id)
                .Select(r => new MessageFaceScore(r.MessageId, r.UserId, r.NegativePhraseCount, r.NegativeReactionCount, r.PositivePhraseCount, r.PositiveReactionCount))
                .SingleAsync(ct);
        }

        async ValueTask<MessageFaceScore?> IMessageFaceScoresRepository.GetMessageFaceScoreOrDefaultAsync(Guid id, CancellationToken ct)
        {
            return await MessageFaceScores
                .Where(r => r.MessageId == id)
                .Select(r => new MessageFaceScore(r.MessageId, r.UserId, r.NegativePhraseCount, r.NegativeReactionCount, r.PositivePhraseCount, r.PositiveReactionCount))
                .SingleOrDefaultAsync(ct);
        }

        async ValueTask<MessageFaceScore[]> IMessageFaceScoresRepository.GetMessageFaceScoresByUserIdAsync(Guid userId, CancellationToken ct)
        {
            return await MessageFaceScores
                .Where(r => r.UserId == userId)
                .Select(r => new MessageFaceScore(r.MessageId, r.UserId, r.NegativePhraseCount, r.NegativeReactionCount, r.PositivePhraseCount, r.PositiveReactionCount))
                .ToArrayAsync(ct);
        }

        async ValueTask<UserFaceCount> IMessageFaceScoresRepository.GetUserFaceCountAsync(Guid userId, CancellationToken ct)
        {
            return await Database.SqlQuery<UserFaceCount>($"""
                SELECT
                    COALESCE(`m`.`user_id`, {userId})               AS `UserId`,
                    COALESCE(SUM(`m`.`negative_phrase_count`)  , 0) AS `NegativePhraseCount`,
                    COALESCE(SUM(`m`.`negative_reaction_count`), 0) AS `NegativeReactionCount`,
                    COALESCE(SUM(`m`.`positive_phrase_count`)  , 0) AS `PositivePhraseCount`,
                    COALESCE(SUM(`m`.`positive_reaction_count`), 0) AS `PositiveReactionCount`
                FROM
                    `message_face_scores` AS `m`
                WHERE
                    `m`.`user_id` = {userId}
                """)
                .SingleAsync(ct);
        }

        async ValueTask<UserFaceCount[]> IMessageFaceScoresRepository.GetUserFaceCountsAsync(CancellationToken ct)
        {
            return await MessageFaceScores
                .GroupBy(s => s.UserId)
                .Select(static g => new UserFaceCount(
                    g.Key,
                    (uint)g.Sum(s => s.NegativePhraseCount),
                    (uint)g.Sum(s => s.NegativeReactionCount),
                    (uint)g.Sum(s => s.PositivePhraseCount),
                    (uint)g.Sum(s => s.PositiveReactionCount)
                    ))
                .ToArrayAsync(ct);
        }

        async ValueTask<MessageFaceScore> IMessageFaceScoresRepository.AddOrUpdateMessageFaceScoreAsync(Guid messageId, Func<MessageFaceScore?, CancellationToken, ValueTask<MessageFaceScore>> configureAsync, CancellationToken ct)
        {
            await using var tx = await Database.BeginTransactionAsync(System.Data.IsolationLevel.ReadCommitted, ct);
            try
            {
                var currentEntity = await MessageFaceScores.FindAsync([messageId], ct);
                var updated = await configureAsync.Invoke(currentEntity?.ToDomainObject(), ct);
                if (currentEntity is null)
                {
                    _ = await MessageFaceScores.AddAsync(new Models.MessageFaceScore()
                    {
                        MessageId = updated.MessageId,
                        UserId = updated.AuthorId,
                        NegativePhraseCount = updated.NegativePhraseCount,
                        NegativeReactionCount = updated.NegativeReactionCount,
                        PositivePhraseCount = updated.PositivePhraseCount,
                        PositiveReactionCount = updated.PositiveReactionCount
                    }, ct);
                }
                else
                {
                    currentEntity.MessageId = updated.MessageId;
                    currentEntity.UserId = updated.AuthorId;
                    currentEntity.NegativePhraseCount = updated.NegativePhraseCount;
                    currentEntity.NegativeReactionCount = updated.NegativeReactionCount;
                    currentEntity.PositivePhraseCount = updated.PositivePhraseCount;
                    currentEntity.PositiveReactionCount = updated.PositiveReactionCount;
                }
                await SaveChangesAsync(ct);
                await tx.CommitAsync(ct);
                return updated;
            }
            catch
            {
                await tx.RollbackAsync(ct);
                throw;
            }
        }
    }
}
