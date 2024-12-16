using Microsoft.Extensions.Logging;

namespace BotTidus.TraqClient
{
    public class ClientTraqService(ILogger<ClientTraqService> logger) : IClientTraqService
    {
        readonly HttpClient _client = new();

        readonly ILogger<ClientTraqService> _logger = logger;

        public void Dispose()
        {
            _client.Dispose();
            GC.SuppressFinalize(this);
        }

        public Task StartAsync(CancellationToken ct)
        {
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken ct)
        {
            return Task.CompletedTask;
        }
    }
}
