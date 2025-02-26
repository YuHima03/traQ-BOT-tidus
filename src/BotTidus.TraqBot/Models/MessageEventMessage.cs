using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace BotTidus.TraqBot.Models
{
    public class MessageEventMessage
    {
        [JsonPropertyName("id")]
        public Guid Id { get; set; }

        [JsonPropertyName("channelId")]
        public Guid ChannelId { get; set; }

        [JsonPropertyName("createdAt")]
        public DateTimeOffset CreatedAt { get; set; }

        [JsonPropertyName("plainText")]
        [NotNull]
        public string? PlainText { get; set; }

        [JsonPropertyName("text")]
        [NotNull]
        public string? Text { get; set; }

        [JsonPropertyName("updatedAt")]
        public DateTimeOffset UpdatedAt { get; set; }
    }
}
