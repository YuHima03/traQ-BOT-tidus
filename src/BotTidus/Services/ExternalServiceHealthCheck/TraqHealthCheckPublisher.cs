namespace BotTidus.Services.ExternalServiceHealthCheck
{
    internal class TraqHealthCheckPublisher()
    {
        public DateTimeOffset LastCheckedAt { get; internal set; }

        public TraqStatus CurrentStatus { get; internal set; } = TraqStatus.Unknown;
    }
}
