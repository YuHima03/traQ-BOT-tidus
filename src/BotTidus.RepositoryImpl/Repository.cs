using BotTidus.Domain;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics.CodeAnalysis;

namespace BotTidus.RepositoryImpl
{
    public sealed partial class Repository(DbContextOptions<Repository> options) : DbContext(options), IRepository
    {
        [NotNull]
        DbSet<Models.MessageFaceScore>? MessageFaceScores { get; set; }

        [NotNull]
        DbSet<Models.DiscordWebhook>? DiscordWebhooks { get; set; }
    }
}
