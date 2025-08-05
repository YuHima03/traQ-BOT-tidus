using BotTidus.Domain.MessageFaceScores;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace BotTidus.Infrastructure.Repository
{
    public partial class BotDbContext : IMessageFaceScoresRepository
    {
        public async ValueTask AddMessageFaceScoreAsync(MessageFaceScore score, CancellationToken ct)
        {
            _ = await MessageFaceScores.AddAsync(new MessageFaceScore_Repo()
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

        public async ValueTask<MessageFaceScore> AddOrUpdateMessageFaceScoreAsync(Guid messageId, Func<MessageFaceScore?, CancellationToken, ValueTask<MessageFaceScore>> configureAsync, CancellationToken ct)
        {
            await using var tx = await Database.BeginTransactionAsync(System.Data.IsolationLevel.ReadCommitted, ct);
            try
            {
                var ctx = this;
                var mid = messageId;
                var current = await ctx.MessageFaceScores
                    .AsNoTracking()
                    .Where(s => s.MessageId == mid)
                    .Select(MessageFaceScoreExtensions.ToDomainExpression)
                    .AsAsyncEnumerable()
                    .FirstOrDefaultAsync(ct);
                var updated = await configureAsync(current, ct);

                if (current is null)
                {
                    await MessageFaceScores.AddAsync(new MessageFaceScore_Repo()
                    {
                        MessageId = updated.MessageId,
                        UserId = updated.AuthorId,
                        NegativePhraseCount = updated.NegativePhraseCount,
                        NegativeReactionCount = updated.NegativeReactionCount,
                        PositivePhraseCount = updated.PositivePhraseCount,
                        PositiveReactionCount = updated.PositiveReactionCount
                    }, ct);
                    await SaveChangesAsync(ct);
                }
                else
                {
                    var rowsAffected = await Task.Run(() =>
                    {
                        var (np, nr, pp, pr) = (updated.NegativePhraseCount, updated.NegativeReactionCount, updated.PositivePhraseCount, updated.PositiveReactionCount);
                        var uid = updated.AuthorId;
                        return ctx.MessageFaceScores
                            .AsNoTracking()
                            .Where(s => s.MessageId == mid)
                            .ExecuteUpdate(s => s
                                .SetProperty(m => m.NegativePhraseCount, np)
                                .SetProperty(m => m.NegativeReactionCount, nr)
                                .SetProperty(m => m.PositivePhraseCount, pp)
                                .SetProperty(m => m.PositiveReactionCount, pr)
                                .SetProperty(m => m.UserId, uid));
                    }, ct);

                    if (rowsAffected == 0)
                    {
                        throw new InvalidOperationException($"No rows were affected when updating face score for message {messageId}. The message may have been deleted or modified by another process.");
                    }
                }
                await tx.CommitAsync(ct);
                return updated;
            }
            catch
            {
                await tx.RollbackAsync(ct);
                throw;
            }
        }

        public ValueTask DeleteMessageFaceScoreAsync(Guid messageId, CancellationToken ct)
        {
            return new(Task.Run(() =>
            {
                var ctx = this;
                var mid = messageId;
                ctx.MessageFaceScores
                    .AsNoTracking()
                    .Where(s => s.MessageId == mid)
                    .ExecuteDelete();
            }, ct));
        }

        public async ValueTask<MessageFaceScore> GetMessageFaceScoreAsync(Guid messageId, CancellationToken ct)
        {
            return await GetMessageFaceScoreOrDefaultAsync(messageId, ct) ?? throw new KeyNotFoundException($"MessageFaceScore with ID {messageId} not found.");
        }

        public async ValueTask<MessageFaceScore?> GetMessageFaceScoreOrDefaultAsync(Guid messageId, CancellationToken ct)
        {
            var ctx = this;
            var mid = messageId;
            return await ctx.MessageFaceScores
                .AsNoTracking()
                .Where(s => s.MessageId == mid)
                .Select(MessageFaceScoreExtensions.ToDomainExpression)
                .AsAsyncEnumerable()
                .FirstOrDefaultAsync(ct);
        }

        public async ValueTask<MessageFaceScore[]> GetMessageFaceScoresByUserIdAsync(Guid userId, CancellationToken ct)
        {
            var ctx = this;
            var uid = userId;
            return await ctx.MessageFaceScores
                .AsNoTracking()
                .Where(r => r.UserId == uid)
                .Select(MessageFaceScoreExtensions.ToDomainExpression)
                .AsAsyncEnumerable()
                .ToArrayAsync(ct);
        }

        public async ValueTask<UserFaceCount> GetUserFaceCountAsync(Guid userId, CancellationToken ct)
        {
            var ctx = this;
            var uid = userId;
            var rows = ctx.MessageFaceScores
                .AsNoTracking()
                .Where(s => s.UserId == uid)
                .Select(MessageFaceScoreExtensions.ToDomainExpression) // Do not remove this line, it is necessary for EF Core to compile the query correctly.
                .AsAsyncEnumerable();
            (uint np, uint nr, uint pp, uint pr) = (0, 0, 0, 0);
            await foreach (var r in rows.WithCancellation(ct))
            {
                np += r.NegativePhraseCount;
                nr += r.NegativeReactionCount;
                pp += r.PositivePhraseCount;
                pr += r.PositiveReactionCount;
            }
            return new(uid, np, nr, pp, pr);
        }

        public async ValueTask<UserFaceCount[]> GetUserFaceCountsAsync(CancellationToken ct)
        {
            var ctx = this;
            return await ctx.MessageFaceScores
                .AsNoTracking()
                .GroupBy(s => s.UserId)
                .Select(g => new UserFaceCount(
                    g.Key,
                    // Do not remove the cast (long) to avoid errors in EFCore pre-compilation.
                    (uint)g.Sum(s => (long)s.NegativePhraseCount),
                    (uint)g.Sum(s => (long)s.NegativeReactionCount),
                    (uint)g.Sum(s => (long)s.PositivePhraseCount),
                    (uint)g.Sum(s => (long)s.PositiveReactionCount)
                ))
                .AsAsyncEnumerable()
                .ToArrayAsync(ct);
        }
    }

    static class MessageFaceScoreExtensions
    {
        /// <summary>
        /// An expression equivalent to the <see cref="ToDomain(MessageFaceScore_Repo)"/> method.
        /// </summary>
        public static readonly Expression<Func<MessageFaceScore_Repo, MessageFaceScore>> ToDomainExpression = model => new MessageFaceScore(
            model.MessageId,
            model.UserId,
            model.NegativePhraseCount,
            model.NegativeReactionCount,
            model.PositivePhraseCount,
            model.PositiveReactionCount
        );

        public static MessageFaceScore ToDomain(this MessageFaceScore_Repo model)
        {
            return new MessageFaceScore(
                model.MessageId,
                model.UserId,
                model.NegativePhraseCount,
                model.NegativeReactionCount,
                model.PositivePhraseCount,
                model.PositiveReactionCount
            );
        }
    }
}
