using BotTidus.Domain.DiscordWebhook;
using Microsoft.EntityFrameworkCore;

namespace BotTidus.RepositoryImpl
{
    sealed partial class Repository : IDiscordWebhooksRepository
    {
        public async ValueTask<DiscordWebhook[]> GetDiscordWebhooksAsync(bool includeDisabled, CancellationToken ct)
        {
            var q = includeDisabled switch
            {
                true => DiscordWebhooks.AsQueryable(),
                false => DiscordWebhooks.Where(x => x.IsEnabled)
            };
            return await q.Select(x => x.ToDomainObject()).ToArrayAsync(ct).ConfigureAwait(false);
        }
    }
}
