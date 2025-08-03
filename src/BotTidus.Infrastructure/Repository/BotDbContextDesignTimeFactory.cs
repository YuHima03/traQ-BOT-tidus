using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace BotTidus.Infrastructure.Repository
{
    public sealed class BotDbContextDesignTimeFactory : IDesignTimeDbContextFactory<BotDbContext>
    {
        public BotDbContext CreateDbContext(string[] args)
        {
            DbContextOptionsBuilder<BotDbContext> optionsBuilder = new();
            optionsBuilder.UseMySql(MariaDbServerVersion.LatestSupportedServerVersion);
            return new(optionsBuilder.Options);
        }
    }
}
