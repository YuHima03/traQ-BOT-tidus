namespace BotTidus.TraqClient
{
    internal sealed class TraqAuthOptions
    {
        public required AuthorizationMethods AuthorizationMethod { get; init; }
        public required string? Token { get; init; }
    }
}
