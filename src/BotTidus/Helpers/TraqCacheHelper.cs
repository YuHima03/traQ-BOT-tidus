using Microsoft.Extensions.Caching.Memory;
using System.Diagnostics.CodeAnalysis;

namespace BotTidus.Helpers
{
    static class TraqCacheHelper
    {
        static readonly TimeSpan UserDetailExpiration = TimeSpan.FromMinutes(3);
        static readonly TimeSpan UserNameMappingExpiration = TimeSpan.FromDays(1);
        static readonly TimeSpan UserAbstractExpiration = TimeSpan.FromDays(1);
        static readonly TimeSpan ChannelPathExpiration = TimeSpan.FromHours(2);

        const string Prop_User = "traq.user";
        const string Prop_UsernameMapping = "traq.usernameMap";
        const string Prop_UserAbstract = "traq.userAbstract";
        const string Prop_ChannelPath = "traq.channel.path";

        static TItem Set<TKey, TItem>(this IMemoryCache cache, string prop, TKey key, TItem value, TimeSpan absoluteExpiration)
        {
            return cache.Set($"{prop}:{key}", value, absoluteExpiration);
        }

        static bool TryGetValue<T>(this IMemoryCache cache, string prop, object key, out T? value)
        {
            return cache.TryGetValue($"{prop}:{key}", out value);
        }

        public static async ValueTask<Traq.Models.UserDetail> GetCachedUserAsync(this Traq.Users.UsersRequestBuilder users, Guid id, IMemoryCache cache, CancellationToken ct)
        {
            if (cache.TryGetValue<Traq.Models.UserDetail>(Prop_User, id, out var user) && user is not null)
            {
                return user;
            }
            user = await users[id].GetAsync(null, ct);
            if (user is not null)
            {
                cache.Set(Prop_User, id, user, UserDetailExpiration);
            }
            return user!;
        }

        public static async ValueTask<UserPermanentInfo> GetCachedUserAbstractAsync(this Traq.Users.UsersRequestBuilder users, Guid id, IMemoryCache cache, CancellationToken ct)
        {
            if (cache.TryGetValue<UserPermanentInfo>(Prop_UserAbstract, id, out var user) && user is not null)
            {
                return user;
            }
            var userDetail = await users.GetCachedUserAsync(id, cache, ct) ?? throw new Exception("User is not found.");
            user = new UserPermanentInfo
            {
                Id = userDetail.Id.GetValueOrDefault(),
                Name = userDetail.Name,
                IsBot = userDetail.Bot.GetValueOrDefault()
            };
            cache.Set(Prop_UserAbstract, id, user, UserAbstractExpiration);
            return user;
        }

        public static async ValueTask<Guid> GetCachedUserIdAsync(this Traq.Users.UsersRequestBuilder users, string name, IMemoryCache cache, CancellationToken ct)
        {
            var nameLower = name.ToLower();
            if (cache.TryGetValue<Guid>(Prop_UsernameMapping, nameLower, out var id))
            {
                return id;
            }
            var user = (await users.GetAsync(conf => conf.QueryParameters.Name = name, ct))?.SingleOrDefault();
            if (user is null)
            {
                throw new Exception("User is not found.");
            }
            var userId = user.Id!.Value;
            cache.Set(Prop_UsernameMapping, nameLower, userId, UserNameMappingExpiration);
            cache.Set(Prop_UsernameMapping, userId, nameLower, UserNameMappingExpiration);
            return userId;
        }

        public static async ValueTask<string> GetCachedUserNameAsync(this Traq.Users.UsersRequestBuilder users, Guid id, IMemoryCache cache, CancellationToken ct)
        {
            if (!cache.TryGetValue<UserPermanentInfo>(Prop_UserAbstract, id, out var user) || user is null)
            {
                if (cache.TryGetValue<string>(Prop_UsernameMapping, id, out var name))
                {
                    return name!;
                }
                user = await users.GetCachedUserAbstractAsync(id, cache, ct);
            }
            var nameLower = user.Name.ToLower();
            cache.Set(Prop_UsernameMapping, id, nameLower, UserNameMappingExpiration);
            cache.Set(Prop_UsernameMapping, nameLower, id, UserNameMappingExpiration);
            return user.Name;
        }

        public static async ValueTask<string?> TryGetCachedChannelPathAsync(this Traq.Channels.ChannelsRequestBuilder channels, Guid id, IMemoryCache cache, CancellationToken ct)
        {
            if (cache.TryGetValue<string>(Prop_ChannelPath, id, out var p) && p is not null)
            {
                return p;
            }
            var path = await channels[id].Path.GetAsync(null, ct);
            if (path is not null)
            {
                cache.Set(Prop_ChannelPath, id, path.Path, ChannelPathExpiration);
                return path.Path;
            }
            return null;
        }

        public static ValueTask<bool> TryGetCachedUserIdAsync(this Traq.Users.UsersRequestBuilder users, string name, IMemoryCache cache, out ValueTask<Guid> resultTask, CancellationToken ct)
        {
            if (cache.TryGetValue<Guid>(Prop_UsernameMapping, name, out var userId))
            {
                resultTask = ValueTask.FromResult(userId);
                return ValueTask.FromResult(true);
            }
            var task = users.GetAsync(conf => conf.QueryParameters.Name = name, ct).ContinueWith(t => t.Result?.SingleOrDefault()?.Id ?? Guid.Empty);
            resultTask = new(task);
            return new(task.ContinueWith(t => {
                var id = t.Result;
                if (id == Guid.Empty)
                {
                    return false;
                }
                var nameLower = name.ToLower();
                cache.Set(Prop_UsernameMapping, id, nameLower, UserNameMappingExpiration);
                cache.Set(Prop_UsernameMapping, nameLower, id, UserNameMappingExpiration);
                return true;
            }));
        }

        public sealed class UserPermanentInfo
        {
            public Guid Id { get; init; }

            [NotNull]
            public string? Name { get; init; }

            public bool IsBot { get; init; }
        }
    }
}
