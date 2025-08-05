using BotTidus.Domain.DiscordWebhook;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace BotTidus.Infrastructure.Repository
{
    public partial class BotDbContext : IDiscordWebhooksRepository
    {
        public async ValueTask<DiscordWebhook[]> GetDiscordWebhooksAsync(bool includeDisabled, CancellationToken ct)
        {
            var ctx = this;
            return includeDisabled
                ? await ctx.DiscordWebhooks
                    .AsNoTracking()
                    .Select(DiscordWebhookExtensions.ToDomainExpression)
                    .AsAsyncEnumerable()
                    .ToArrayAsync(ct)
                : await ctx.DiscordWebhooks
                    .AsNoTracking()
                    .Where(r => r.IsEnabled)
                    .Select(DiscordWebhookExtensions.ToDomainExpression)
                    .AsAsyncEnumerable()
                    .ToArrayAsync(ct);
        }
    }

    static class DiscordWebhookExtensions
    {
        /// <summary>
        /// An expression equivalent to the <see cref="ToDomain(DiscordWebhook_Repo)"/> method.
        /// </summary>
        public static readonly Expression<Func<DiscordWebhook_Repo, DiscordWebhook>> ToDomainExpression = model => new DiscordWebhook(
            model.Id,
            Uri.IsWellFormedUriString(model.PostUrl, UriKind.Absolute) ? new Uri(model.PostUrl) : null,
            model.UserId,
            model.IsEnabled,
            (MessageFilter)model.NotifiesOnFlags,
            model.CreatedAt,
            model.UpdatedAt
        );
    }
}
