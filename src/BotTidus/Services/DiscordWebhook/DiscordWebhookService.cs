using BotTidus.Configurations;
using BotTidus.Domain;
using BotTidus.Helpers;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json.Serialization;
using Traq;
using Traq.Extensions.Messages;
using Traq.Model;

namespace BotTidus.Services.DiscordWebhook
{
    internal class DiscordWebhookService(
        IRepositoryFactory repositoryFactory,
        ITraqApiClient traq,
        IOptions<TraqBotOptions> botOptions,
        IMemoryCache cache,
        ILogger<DiscordWebhookService> logger,
        IServiceProvider services
        )
        : RecentMessageCollectingService(services, TimeSpan.FromSeconds(30))
    {
        readonly TraqBotOptions _botOptions = botOptions.Value;
        readonly string _traqHost = new Uri(botOptions.Value.TraqApiBaseAddress!).Host;
        readonly HttpClient _httpClient = new();

        static readonly MediaTypeHeaderValue JsonMediaType = new("application/json");

        protected override async ValueTask OnCollectAsync(ActivityTimelineMessage[] messages, CancellationToken ct)
        {
            if (messages.Length == 0)
            {
                return;
            }

            await using var repo = await repositoryFactory.CreateRepositoryAsync(ct);
            var webhooks = (await repo.GetDiscordWebhooksAsync(false, ct)).GroupBy(x => x.UserId);

            List<Task> webhookTasks = [];

            HashSet<Guid> mentionedUsers = [];
            HashSet<Guid> mentionedGroups = [];
            HashSet<Guid> citedUsers = [];
            List<Task<Message>> citedMessageTasks = [];
            foreach (var msg in messages)
            {
                mentionedUsers.Clear();
                mentionedGroups.Clear();
                citedUsers.Clear();
                citedMessageTasks.Clear();

                var content = msg.Content;
                StringBuilder plainTextBuilder = new(content.Length);

                using (MessageElementEnumerator elements = new(content))
                {
                    foreach (var e in elements)
                    {
                        if (e.Kind == MessageElementKind.Embedding)
                        {
                            var embedding = e.GetEmbedding();
                            switch (embedding.Type)
                            {
                                case EmbeddingType.UserMention:
                                    mentionedUsers.Add(embedding.EmbeddedId);
                                    plainTextBuilder.Append(embedding.DisplayText);
                                    break;

                                case EmbeddingType.GroupMention:
                                    mentionedGroups.Add(embedding.EmbeddedId);
                                    plainTextBuilder.Append(embedding.DisplayText);
                                    break;

                                case EmbeddingType.Channel:
                                    plainTextBuilder.Append(embedding.DisplayText);
                                    break;
                            }
                        }
                        else if (e.Kind == MessageElementKind.Url)
                        {
                            try
                            {
                                var uri = e.GetUrl().GetUri(Uri.UriSchemeHttps);
                                var uriString = uri.ToString();
                                if (uri.Host.Equals(_traqHost, StringComparison.OrdinalIgnoreCase)
                                    && uriString.Length >= 45
                                    && uriString[^45..^37] == "messages"
                                    && Guid.TryParse(uriString[^36..], out var msgId))
                                {
                                    citedMessageTasks.Add(traq.MessageApi.GetMessageAsync(msgId, ct));
                                }
                                plainTextBuilder.Append(uri.OriginalString);
                            }
                            catch (Exception ex)
                            {
                                logger.LogError(ex, "Failed to get embed uri and get cited message.");
                            }
                        }
                        else
                        {
                            plainTextBuilder.Append(e.RawText);
                        }
                    }
                }

                foreach (var cited in citedMessageTasks)
                {
                    try
                    {
                        citedUsers.Add((await cited).UserId);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Failed to get cited message.");
                    }
                }

                var authorName = await traq.UserApi.GetCachedUserNameAsync(msg.UserId, cache, ct);
                DiscordWebhookMessage webhookMessage = new()
                {
                    Embed = [
                        new() {
                            Author = new() {
                                Name = authorName,
                                IconUrl = string.Concat(_botOptions.TraqApiBaseAddress.AsSpan().TrimEnd('/'), "/public/icon/", authorName)
                            },
                            Description = plainTextBuilder.ToString()
                        }
                    ],
                };

                foreach (var g in webhooks)
                {
                    var userId = g.Key;
                    var userDetail = await traq.UserApi.GetCachedUserAsync(userId, cache, ct);
                    foreach (var w in g)
                    {
                        if (w.PostUrl is null)
                        {
                            continue;
                        }

                        if (((w.NotifiesOn & Domain.DiscordWebhook.MessageFilter.UserMentioned) != 0 && mentionedUsers.Contains(userId))
                            || ((w.NotifiesOn & Domain.DiscordWebhook.MessageFilter.GroupMentioned) != 0 && mentionedGroups.Intersect(userDetail.Groups).Any())
                            || ((w.NotifiesOn & Domain.DiscordWebhook.MessageFilter.UserMessageCited) != 0 && citedUsers.Contains(userId)))
                        {
                            webhookTasks.Add(Task.Run(async () =>
                            {
                                try
                                {
                                    var reqContent = JsonContent.Create(webhookMessage, JsonMediaType);
                                    var res = await _httpClient.PostAsync(w.PostUrl, reqContent, ct);
                                    if (!res.IsSuccessStatusCode)
                                    {
                                        logger.LogWarning("Discord webhook returned {StatusCode} for user {UserId} with message: {MessageId}", res.StatusCode, userId, msg.UserId);
                                    }
                                }
                                catch (Exception ex)
                                {
                                    logger.LogError(ex, "Failed to send Discord webhook message.");
                                }
                            }, ct));
                        }
                    }
                }
            }

            await Task.WhenAll(webhookTasks);
        }
    }

    file sealed class DiscordWebhookMessage
    {
        [JsonPropertyName("embeds")]
        public required DiscordWebhookMessageEmbed[] Embed { get; init; }
    }

    file sealed class DiscordWebhookMessageEmbed
    {
        [JsonPropertyName("author")]
        public required DiscordWebhookMessageEmbedAuthor Author { get; init; }

        [JsonPropertyName("description")]
        public required string Description { get; init; }
    }

    file sealed class DiscordWebhookMessageEmbedAuthor
    {
        [JsonPropertyName("name")]
        public required string Name { get; init; }

        [JsonPropertyName("icon_url")]
        public required string IconUrl { get; init; }
    }
}
