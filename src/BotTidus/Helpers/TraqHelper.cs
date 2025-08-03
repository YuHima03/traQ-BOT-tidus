using System.Runtime.InteropServices;

namespace BotTidus.Helpers
{
    static class TraqHelper
    {
        const int MaxSearchMessageLimit = 1_000_000;

        public static async ValueTask AddManyMessageStampAsync(this Traq.Messages.MessagesRequestBuilder messages, Guid messageId, Guid stampId, int count, CancellationToken ct)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(count);
            if (count == 0)
            {
                return;
            }

            (int quot, int rem) = Math.DivRem(count, 100);
            Traq.Models.PostMessageStampRequest req = new() { Count = rem };
            if (rem != 0)
            {
                await messages[messageId].Stamps[stampId].PostAsync(req, null, ct);
            }
            req.Count = 100;
            for (int i = 0; i < quot; i++)
            {
                await messages[messageId].Stamps[stampId].PostAsync(req, null, ct);
            }
        }

        public static ValueTask<bool> TryGetUserIdFromNameAsync(this Traq.Users.UsersRequestBuilder users, string username, out ValueTask<Traq.Models.User> resultTask, CancellationToken ct)
        {
            var task = users.GetAsync(conf => conf.QueryParameters.Name = username, ct).ContinueWith(t => t.Result?.SingleOrDefault());
            resultTask = new(task!);
            return new(task.ContinueWith(t => t.Result is not null));
        }

        public static async ValueTask<List<Traq.Models.Message>> SearchManyMessagesAsync(this Traq.Messages.MessagesRequestBuilder messages, SearchQuery query, CancellationToken ct)
        {
            var limit = query.Limit ?? MaxSearchMessageLimit; // request limit is 10,000.
            ArgumentOutOfRangeException.ThrowIfNegative(limit);
            ArgumentOutOfRangeException.ThrowIfGreaterThan(limit, MaxSearchMessageLimit);

            var before = query.Before ?? DateTimeOffset.UtcNow;

            Traq.Messages.MessagesRequestBuilder.MessagesRequestBuilderGetQueryParameters queryParams = new()
            {
                Word = query.Word,
                After = query.After,
                Before = before,
                In = query.ChannelId,
                To = query.MentionedUsersId is not null ? [.. query.MentionedUsersId] : [],
                From = query.AuthorsId is not null ? [.. query.AuthorsId] : [],
                Citation = query.CitedMessageId,
                Bot = query.AuthorIsBot,
                HasURL = query.HasUrl,
                HasAttachments = query.HasAttachments,
                HasImage = query.HasImage,
                HasVideo = query.HasVideo,
                HasAudio = query.HasAudio,
                Limit = 100,
                Offset = 0,
                SortAsGetSortQueryParameterType = null
            };
            List<Traq.Models.Message> result = [];            
            for (int requestCount = 0; requestCount < MaxSearchMessageLimit / 100; requestCount++)
            {
                var reqRes = await messages.GetAsMessagesGetResponseAsync(conf => conf.QueryParameters = queryParams, ct);
                if (reqRes?.Hits?.Count is not > 0)
                {
                    return result;
                }
                result.AddRange(CollectionsMarshal.AsSpan(reqRes.Hits));
                if (reqRes.Hits.Count < 100)
                {
                    return result;
                }
                before = result[^1].CreatedAt!.Value - TimeSpan.FromMicroseconds(1);
            }
            throw new Exception("Exceeded maximum search message limit.");
        }

        public record SearchQuery(
            string? Word = null,
            DateTimeOffset? After = null,
            DateTimeOffset? Before = null,
            Guid? ChannelId = null,
            List<Guid>? MentionedUsersId = null,
            List<Guid>? AuthorsId = null,
            Guid? CitedMessageId = null,
            bool? AuthorIsBot = null,
            bool? HasUrl = null,
            bool? HasAttachments = null,
            bool? HasImage = null,
            bool? HasVideo = null,
            bool? HasAudio = null,
            long? Limit = null
            );
    }
}
