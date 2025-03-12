using BotTidus.Domain;
using Microsoft.EntityFrameworkCore;

namespace BotTidus.RepositoryImpl
{
    public class RepositoryFactory(IDbContextFactory<Repository> factory) : IRepositoryFactory
    {
        IDbContextFactory<Repository> _factory = factory;

        public async Task<IRepository> CreateRepositoryAsync(CancellationToken cancellationToken = default)
        {
            return await _factory.CreateDbContextAsync(cancellationToken);
        }
    }
}
