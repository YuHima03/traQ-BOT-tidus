using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BotTidus.TraqClient.Models
{
    public sealed class Traq(HttpClient client)
    {
        readonly HttpClient _client = client;

        public required Uri BaseUri { get; init; }

        public ChannelList PublicChannels { get; } = new ChannelList(client);
    }
}
