using BotTidus.Domain;
using Microsoft.EntityFrameworkCore;

namespace BotTidus.RepositoryImpl
{
    public sealed partial class Repository(DbContextOptions<Repository> options) : DbContext(options), IRepository
    {
        DbSet<Models.MessageFaceScore> MessageFaceScores { get; set; }

        DbSet<Models.DiscordWebhook> DiscordWebhooks { get; set; }
    }
}
