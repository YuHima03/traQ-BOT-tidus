using BotTidus.Domain;
using Microsoft.EntityFrameworkCore;

namespace BotTidus.Infrastructure.Repository
{
    public sealed class BotDbContextDefaultFactory(IDbContextFactory<BotDbContext> factory) : IRepositoryFactory
    {
        public async Task<IRepository> CreateRepositoryAsync(CancellationToken cancellationToken = default)
        {
            return await factory.CreateDbContextAsync(cancellationToken);
        }
    }
}
