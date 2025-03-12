using BotTidus.Domain;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics.CodeAnalysis;

namespace BotTidus.RepositoryImpl
{
    public sealed partial class Repository(DbContextOptions options) : DbContext(options), IRepository
    {
        [NotNull]
        DbSet<Models.MessageFaceScore>? MessageFaceScores { get; set; }
    }
}
