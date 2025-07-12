using Microsoft.Extensions.Caching.Memory;
using System.Diagnostics.CodeAnalysis;
using System.Net;

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

        public static async ValueTask<Traq.Model.UserDetail> GetCachedUserAsync(this Traq.Api.IUserApi api, Guid id, IMemoryCache cache, CancellationToken ct)
        {
            if (cache.TryGetValue<Traq.Model.UserDetail>(Prop_User, id, out var user) && user is not null)
            {
                return user;
            }
            user = await api.GetUserAsync(id, ct);
            cache.Set(Prop_User, id, user, UserDetailExpiration);
            return user;
        }

        public static async ValueTask<UserPermanentInfo> GetCachedUserAbstractAsync(this Traq.Api.IUserApi api, Guid id, IMemoryCache cache, CancellationToken ct)
        {
            if (cache.TryGetValue<UserPermanentInfo>(Prop_UserAbstract, id, out var user) && user is not null)
            {
                return user;
            }
            var userDetail = await api.GetCachedUserAsync(id, cache, ct);
            user = new UserPermanentInfo
            {
                Id = userDetail.Id,
                Name = userDetail.Name,
                IsBot = userDetail.Bot
            };
            cache.Set(Prop_UserAbstract, id, user, UserAbstractExpiration);
            return user;
        }

        public static async ValueTask<Guid> GetCachedUserIdAsync(this Traq.Api.IUserApi api, string name, IMemoryCache cache, CancellationToken ct)
        {
            var nameLower = name.ToLower();
            if (cache.TryGetValue<Guid>(Prop_UsernameMapping, nameLower, out var id))
            {
                return id;
            }
            var user = (await api.GetUsersAsync(null, name, ct)).Single();
            cache.Set(Prop_UsernameMapping, nameLower, user.Id, UserNameMappingExpiration);
            cache.Set(Prop_UsernameMapping, user.Id, nameLower, UserNameMappingExpiration);
            return user.Id;
        }

        public static async ValueTask<string> GetCachedUserNameAsync(this Traq.Api.IUserApi api, Guid id, IMemoryCache cache, CancellationToken ct)
        {
            if (!cache.TryGetValue<UserPermanentInfo>(Prop_UserAbstract, id, out var user) || user is null)
            {
                if (cache.TryGetValue<string>(Prop_UsernameMapping, id, out var name))
                {
                    return name!;
                }
                user = await api.GetCachedUserAbstractAsync(id, cache, ct);
            }
            var nameLower = user.Name.ToLower();
            cache.Set(Prop_UsernameMapping, id, nameLower, UserNameMappingExpiration);
            cache.Set(Prop_UsernameMapping, nameLower, id, UserNameMappingExpiration);
            return user.Name;
        }

        public static async ValueTask<string?> TryGetCachedChannelPathAsync(this Traq.Api.IChannelApi api, Guid id, IMemoryCache cache, CancellationToken ct)
        {
            if (cache.TryGetValue<string>(Prop_ChannelPath, id, out var p) && p is not null)
            {
                return p;
            }
            try
            {
                var path = await api.GetChannelPathAsync(id, ct);
                if (path is not null)
                {
                    cache.Set(Prop_ChannelPath, id, path.Path, ChannelPathExpiration);
                    return path.Path;
                }
                return null;
            }
            catch (Traq.Client.ApiException ex) when (ex.ErrorCode == (int)HttpStatusCode.NotFound)
            {
                return null;
            }
        }

        public static ValueTask<bool> TryGetCachedUserIdAsync(this Traq.Api.IUserApi api, string name, IMemoryCache cache, out ValueTask<Guid> resultTask, CancellationToken ct)
        {
            if (cache.TryGetValue<Guid>(Prop_UsernameMapping, name, out var userId))
            {
                resultTask = ValueTask.FromResult(userId);
                return ValueTask.FromResult(true);
            }
            var task = api.GetUsersAsync(null, name, ct).ContinueWith(t => t.Result.SingleOrDefault()?.Id);
            resultTask = new(task.ContinueWith(t => t.Result.GetValueOrDefault()));
            return new(task.ContinueWith(t =>
            {
                var res = t.Result;
                if (!res.HasValue)
                {
                    return false;
                }
                var nameLower = name.ToLower();
                cache.Set(Prop_UsernameMapping, res.Value, nameLower, UserNameMappingExpiration);
                cache.Set(Prop_UsernameMapping, nameLower, res.Value, UserNameMappingExpiration);
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
