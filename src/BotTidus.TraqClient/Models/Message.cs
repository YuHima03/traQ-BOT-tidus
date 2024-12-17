using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace BotTidus.TraqClient.Models
{
    public sealed class Message
    {

    }

    public sealed class PostMessageRequest
    {
        [JsonPropertyName("content")]
        [NotNull]
        public string? Text { get; set; }

        [JsonPropertyName("embed")]
        public bool AutoEmbedding { get; set; }
    }
}
