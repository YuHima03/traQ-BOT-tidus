using System.Net.Http.Json;

namespace BotTidus.TraqClient.Models
{
    public sealed class ChannelList(HttpClient client) : IAsyncEnumerable<Channel>
    {
        readonly HttpClient _client = client;

        public IAsyncEnumerator<Channel> GetAsyncEnumerator(CancellationToken ct)
        {
            throw new NotImplementedException();
        }

        public async ValueTask<Channel?> FindAsync(string path, CancellationToken ct)
        {
            var ch = await _client.GetFromJsonAsync<GetChannelsResult>($"/api/v3/channels?path={Uri.EscapeDataString(path)}", ct);
            if (ch is null)
            {
                return null;
            }
            var pch = ch.PublicChannel;
            if (pch is not null)
            {
                pch._client = _client;
            }
            return pch;
        }
    }
}
