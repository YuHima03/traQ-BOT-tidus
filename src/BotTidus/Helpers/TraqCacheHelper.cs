using Microsoft.Extensions.Caching.Memory;

namespace BotTidus.Helpers
{
    static class TraqCacheHelper
    {
        static readonly TimeSpan UserInfoExpiration = TimeSpan.FromMinutes(3);

        public static async ValueTask<Traq.Model.UserDetail> GetCachedUserAsync(this Traq.Api.IUserApi api, Guid id, IMemoryCache cache, CancellationToken ct)
        {
            if (cache.TryGetValue<Traq.Model.UserDetail>($"traq.user[{id}]", out var user))
            {
                return user!;
            }
            user = await api.GetUserAsync(id, ct);
            cache.CreateEntry($"traq.user[{id}]").SetValue(user).SetAbsoluteExpiration(UserInfoExpiration);
            return user;
        }
    }
}
