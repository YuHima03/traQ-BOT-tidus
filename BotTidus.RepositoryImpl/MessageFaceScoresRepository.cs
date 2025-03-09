﻿using BotTidus.Domain.MessageFaceScores;
using Microsoft.EntityFrameworkCore;

namespace BotTidus.RepositoryImpl
{
    public sealed partial class Repository
    {
        ValueTask IMessageFaceScoresRepository.AddMessageFaceScoreAsync(MessageFaceScore score, CancellationToken ct)
        {
            throw new NotImplementedException();
        }

        ValueTask<MessageFaceScore> IMessageFaceScoresRepository.GetMessageFaceScoreAsync(Guid id, CancellationToken ct)
        {
            throw new NotImplementedException();
        }

        async ValueTask<MessageFaceScore[]> IMessageFaceScoresRepository.GetMessageFaceScoresByUserIdAsync(Guid userId, CancellationToken ct)
        {
            return await MessageFaceScores
                .Where(r => r.UserId == userId)
                .Select(r => new MessageFaceScore(r.MessageId, r.UserId, r.NegativePhraseCount, r.NegativeReactionCount, r.PositivePhraseCount, r.PositiveReactionCount))
                .ToArrayAsync(ct);
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

        ValueTask IMessageFaceScoresRepository.UpdateMessageFaceScoreAsync(MessageFaceScore score, CancellationToken ct)
        {
            throw new NotImplementedException();
        }
    }
}
