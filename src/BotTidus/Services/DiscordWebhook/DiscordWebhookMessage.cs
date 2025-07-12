using System.Text.Json.Serialization;

namespace BotTidus.Services.DiscordWebhook
{
    sealed class DiscordWebhookMessage
    {
        [JsonPropertyName("username")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Username { get; init; }

        [JsonIgnore]
        public Uri? AvatarUrl { get; init; }

        [JsonPropertyName("avatar_url")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? AvatarUrlString => AvatarUrl?.ToString();

        [JsonPropertyName("content")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Content { get; init; }

        [JsonPropertyName("embeds")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public Embed[]? Embeds { get; init; } = [];

        internal sealed class Embed
        {
            [JsonPropertyName("title")]
            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
            public string? Title { get; init; }

            [JsonPropertyName("description")]
            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
            public string? Description { get; init; }

            [JsonIgnore]
            public Uri? Url { get; init; }

            [JsonPropertyName("url")]
            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
            public string? UrlString => Url?.ToString();

            [JsonIgnore]
            public DateTimeOffset? Timestamp { get; init; }

            [JsonPropertyName("timestamp")]
            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
            public string? TimestampString => Timestamp?.ToString("O");

            [JsonPropertyName("color")]
            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
            public int? Color { get; init; }

            [JsonPropertyName("author")]
            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
            public EmbedAuthor? Author { get; init; }

            [JsonPropertyName("fields")]
            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
            public EmbedField[]? Fields { get; init; } = [];

            [JsonPropertyName("footer")]
            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
            public EmbedFooter? Footer { get; init; }

            [JsonPropertyName("image")]
            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
            public EmbedImage? Image { get; init; }

            [JsonPropertyName("provider")]
            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
            public EmbedProvider? Provider { get; init; }

            [JsonPropertyName("thumbnail")]
            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
            public EmbedImage? Thumbnail { get; init; }
        }

        internal sealed class EmbedAuthor
        {
            [JsonPropertyName("name")]
            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
            public string? Name { get; init; }

            [JsonIgnore]
            public Uri? Url { get; init; }

            [JsonPropertyName("url")]
            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
            public string? UrlString => Url?.ToString();

            [JsonIgnore]
            public Uri? IconUrl { get; init; }

            [JsonPropertyName("icon_url")]
            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
            public string? IconUrlString => IconUrl?.ToString();
        }

        internal sealed class EmbedField
        {
            [JsonPropertyName("name")]
            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
            public string? Name { get; init; }

            [JsonPropertyName("value")]
            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
            public string? Value { get; init; }

            [JsonPropertyName("inline")]
            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
            public bool? Inline { get; init; }
        }

        internal sealed class EmbedFooter
        {
            [JsonPropertyName("text")]
            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
            public string? Text { get; init; }

            [JsonIgnore]
            public Uri? IconUrl { get; init; }

            [JsonPropertyName("icon_url")]
            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
            public string? IconUrlString => IconUrl?.ToString();
        }

        internal sealed class EmbedImage
        {
            [JsonIgnore]
            public Uri? Url { get; init; }

            [JsonPropertyName("url")]
            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
            public string? UrlString => Url?.ToString();
        }

        internal sealed class EmbedProvider
        {
            [JsonPropertyName("name")]
            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
            public string? Name { get; init; }

            [JsonIgnore]
            public Uri? Url { get; init; }

            [JsonPropertyName("url")]
            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
            public string? UrlString => Url?.ToString();
        }
    }
}
