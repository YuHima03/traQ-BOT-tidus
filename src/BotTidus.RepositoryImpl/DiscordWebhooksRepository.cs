using BotTidus.Domain.DiscordWebhook;
using Microsoft.EntityFrameworkCore;

namespace BotTidus.RepositoryImpl
{
    sealed partial class Repository : IDiscordWebhooksRepository
    {
        public async ValueTask<DiscordWebhook[]> GetDiscordWebhooksAsync(bool includeDisabled, CancellationToken ct)
        {
            return includeDisabled
                ? await DiscordWebhooks.Select(x => x.ToDomainObject()).ToArrayAsync(ct)
                : await DiscordWebhooks.Where(x => x.IsEnabled).Select(x => x.ToDomainObject()).ToArrayAsync(ct);
        }
    }
}
