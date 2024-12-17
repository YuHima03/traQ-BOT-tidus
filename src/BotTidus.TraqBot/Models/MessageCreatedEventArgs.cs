using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace BotTidus.TraqBot.Models
{
    public sealed class MessageCreatedEventArgs
    {
        [JsonPropertyName("eventTime")]
        public DateTime DispatchedAt { get; set; }

        [JsonPropertyName("message")]
        [NotNull]
        public MessageEventMessage? Message { get; set; }
    }
}
