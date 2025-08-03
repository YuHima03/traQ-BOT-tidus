using BotTidus.Domain;
using Microsoft.EntityFrameworkCore;

namespace BotTidus.RepositoryImpl
{
    public sealed partial class Repository(DbContextOptions<Repository> options) : DbContext(options), IRepository
    {
        public DbSet<Models.MessageFaceScore> MessageFaceScores { get; set; }

        public DbSet<Models.DiscordWebhook> DiscordWebhooks { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<Models.RepoUserFaceCount>();
        }
    }
}
