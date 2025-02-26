namespace BotTidus.TraqClient
{
    public interface IClientTraqBuilder
    {
        public IClientTraqService Build(IServiceProvider? provider);
        public IClientTraqBuilder SetBaseUri(Uri baseUri);
        public IClientTraqBuilder UseBearerAuthorization(string token);
        public IClientTraqBuilder UseCookieAuthorization(string token);
    }
}
