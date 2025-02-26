using Microsoft.EntityFrameworkCore;
using System.Diagnostics.CodeAnalysis;

namespace BotTidus.Repository
{
    public class RepositoryContext : DbContext
    {
        [NotNull]
        public DbSet<Models.FaceCount>? FaceCounts { get; set; }
    }
}
