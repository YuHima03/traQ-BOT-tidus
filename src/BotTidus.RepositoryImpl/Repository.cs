using BotTidus.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using System.Diagnostics.CodeAnalysis;

namespace BotTidus.RepositoryImpl
{
    [Obsolete("Use BotTidus.Infrastructure.Repository.BotDbContext instead.")]
    public sealed partial class Repository : IRepository
    {
        [NotNull]
        public DbSet<Models.MessageFaceScore>? MessageFaceScores { get; }

        [NotNull]
        public DbSet<Models.DiscordWebhook>? DiscordWebhooks { get; }

        [NotNull]
        public DatabaseFacade? Database { get; }

        public Task SaveChangesAsync(CancellationToken ct = default) => Task.CompletedTask;

        public void Dispose() { }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
