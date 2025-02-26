using System.Net.WebSockets;

namespace BotTidus.TraqClient
{
    public enum AuthorizationMethods
    {
        None, Bearer, Cookie
    }

    public interface IClientTraqService : IDisposable
    {
        public Uri BaseUri { get; }
        public Models.Traq Traq { get; }
        public ClientWebSocket CreateClientWebSocket();
    }
}
