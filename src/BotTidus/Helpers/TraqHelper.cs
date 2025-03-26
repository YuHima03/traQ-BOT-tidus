using System.Runtime.InteropServices;

namespace BotTidus.Helpers
{
    static class TraqHelper
    {
        const int MaxSearchMessageLimit = 1_000_000;

        public static ValueTask<bool> TryGetUserIdFromNameAsync(this Traq.Api.IUserApi api, string username, out ValueTask<Traq.Model.User> resultTask, CancellationToken ct)
        {
            var task = api.GetUsersAsync(null, username, ct).ContinueWith(t => t.Result.SingleOrDefault()!);
            resultTask = new(task);
            return new(task.ContinueWith(t => t.Result is not null));
        }

        public static async ValueTask<List<Traq.Model.Message>> SearchManyMessagesAsync(this Traq.Api.IMessageApiAsync api, SearchQuery query, CancellationToken ct)
        {
            var limit = query.Limit ?? MaxSearchMessageLimit; // request limit is 10,000.
            ArgumentOutOfRangeException.ThrowIfNegative(limit);
            ArgumentOutOfRangeException.ThrowIfGreaterThan(limit, MaxSearchMessageLimit);

            var before = query.Before ?? DateTimeOffset.UtcNow;

            List<Traq.Model.Message> messages = [];
            for (int requestCount = 0; requestCount < MaxSearchMessageLimit / 100; requestCount++)
            {
                var result = await api.SearchMessagesAsync(query.Word, query.After, before, query.ChannelId, query.MentionedUsersId, query.AuthorsId, query.CitedMessageId, query.AuthorIsBot, query.HasUrl, query.HasAttachments, query.HasImage, query.HasVideo, query.HasAudio, 100, 0, null, ct);
                if (result.Hits.Count == 0)
                {
                    return messages;
                }
                messages.AddRange(CollectionsMarshal.AsSpan(result.Hits));
                if (result.Hits.Count < 100)
                {
                    return messages;
                }
                before = messages[^1].CreatedAt - TimeSpan.FromMicroseconds(1);
            }
            throw new Exception("Too many requests.");
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
