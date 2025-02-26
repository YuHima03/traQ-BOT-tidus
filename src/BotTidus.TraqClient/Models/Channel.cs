using System.Diagnostics.CodeAnalysis;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace BotTidus.TraqClient.Models
{
    public sealed class Channel
    {
        [NotNull]
        internal HttpClient? _client { get; set; }

        [JsonPropertyName("id")]
        public Guid Id { get; set; }

        public async ValueTask<Message?> AddMessageAsync(string text, bool autoEmbedding, CancellationToken ct)
        {
            PostMessageRequest req = new()
            {
                Text = text,
                AutoEmbedding = autoEmbedding,
            };
            var content = new StringContent(JsonSerializer.Serialize(req), Encoding.UTF8, "application/json");

            var res = await _client.PostAsync($"/api/v3/channels/{Id}/messages", content, ct);
            if (!res.IsSuccessStatusCode)
            {
                return null;
            }
            return await res.Content.ReadFromJsonAsync<Message?>(ct);
        }
    }

    public sealed class GetChannelsResult
    {
        [JsonPropertyName("public")]
        public Channel? PublicChannel { get; set; }
    }
}
