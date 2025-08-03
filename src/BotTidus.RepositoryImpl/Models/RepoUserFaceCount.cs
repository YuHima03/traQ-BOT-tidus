using BotTidus.Domain.MessageFaceScores;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;

namespace BotTidus.RepositoryImpl.Models
{
    [Keyless]
    public sealed class RepoUserFaceCount
    {
        [Column("negative_phrase_count")]
        public uint NegativePhraseCount { get; set; }

        [Column("negative_reaction_count")]
        public uint NegativeReactionCount { get; set; }

        [Column("positive_phrase_count")]
        public uint PositivePhraseCount { get; set; }

        [Column("positive_reaction_count")]
        public uint PositiveReactionCount { get; set; }

        public UserFaceCount ToDomainObject(Guid userId)
        {
            return new UserFaceCount(
                userId,
                NegativePhraseCount,
                NegativeReactionCount,
                PositivePhraseCount,
                PositiveReactionCount
            );
        }
    }
}
