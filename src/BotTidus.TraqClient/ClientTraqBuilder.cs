using BotTidus.TraqClient.Helpers;
using System.Net;
using System.Net.Http.Headers;

namespace BotTidus.TraqClient
{
    internal sealed class ClientTraqBuilder : IClientTraqBuilder
    {
        private TraqAuthOptions _auth = new() { AuthorizationMethod = AuthorizationMethods.None, Token = null };

        private Uri? _baseUri;

        public IClientTraqService Build(IServiceProvider? provider)
        {
            ArgumentNullException.ThrowIfNull(provider);
            ClientTraqService service = new(provider, CreateClient(), _auth, _baseUri);
            return service;
        }

        private HttpClient CreateClient()
        {
            var (authMethod, authToken) = (_auth.AuthorizationMethod, _auth.Token);

            HttpClientHandler handler = new()
            {
                AllowAutoRedirect = false,
                UseCookies = true,
            };
            if (authMethod == AuthorizationMethods.Cookie)
            {
                handler.CookieContainer.Add(new Cookie()
                {
                    HttpOnly = true,
                    Name = "r_session",
                    Path = "/",
                    Secure = true,
                    Value = ValueMust.NotNull(authToken)
                });
            }

            HttpClient client = new(handler);
            if (authMethod == AuthorizationMethods.Bearer)
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", ValueMust.NotNull(authToken));
            }

            client.BaseAddress = _baseUri;
            return client;
        }

        public IClientTraqBuilder SetBaseUri(Uri baseUri)
        {
            ArgumentNullException.ThrowIfNull(baseUri);
            _baseUri = baseUri;
            return this;
        }

        public IClientTraqBuilder UseBearerAuthorization(string token)
        {
            _auth = new() { AuthorizationMethod = AuthorizationMethods.Bearer, Token = token };
            return this;
        }

        public IClientTraqBuilder UseCookieAuthorization(string token)
        {
            _auth = new() { AuthorizationMethod = AuthorizationMethods.Cookie, Token = token };
            return this;
        }
    }
}
