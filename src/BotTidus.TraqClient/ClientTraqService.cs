using BotTidus.TraqClient.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.WebSockets;

namespace BotTidus.TraqClient
{
    public class ClientTraqService : IClientTraqService
    {
        readonly TraqAuthOptions _authOptions;
        readonly HttpClient _client;
        readonly ILogger<ClientTraqService>? _logger;

        public Uri BaseUri { get; private init; }

        public Models.Traq Traq { get; private init; }

        internal ClientTraqService(IServiceProvider provider, HttpClient client, TraqAuthOptions authOptions, Uri baseUri)
        {
            _authOptions = authOptions;
            _client = client;
            _logger = provider.GetService<ILogger<ClientTraqService>>();

            BaseUri = baseUri;
            Traq = new(client) { BaseUri = baseUri };
        }

        public ClientWebSocket CreateClientWebSocket()
        {
            ClientWebSocket ws = new();

            switch (_authOptions.AuthorizationMethod)
            {
                case AuthorizationMethods.Bearer:
                {
                    ws.Options.SetRequestHeader("Authorization", $"Bearer {ValueMust.NotNull(_authOptions.Token)}");
                    break;
                }
                case AuthorizationMethods.Cookie:
                {
                    CookieContainer cookies = ws.Options.Cookies ?? new();
                    cookies.Add(new Cookie()
                    {
                        HttpOnly = true,
                        Name = "r_session",
                        Path = "/",
                        Secure = true,
                        Value = ValueMust.NotNull(_authOptions.Token),
                    });
                    ws.Options.Cookies = cookies;
                    break;
                }
            }

            return ws;
        }

        public void Dispose()
        {
            _logger?.LogInformation("Disposing the traQ client.");
            _client.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
