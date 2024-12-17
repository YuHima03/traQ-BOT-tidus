using BotTidus.TraqClient;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Net.WebSockets;

namespace BotTidus.TraqBot
{
    public class TraqWebSocketBotService : BackgroundService
    {
        readonly ILogger<TraqWebSocketBotService>? _logger;
        readonly IClientTraqService _traq;
        readonly ClientWebSocket _ws;

        public TraqWebSocketBotService(IServiceProvider provider)
        {
            _logger = provider.GetService<ILogger<TraqWebSocketBotService>>();
            _traq = provider.GetRequiredService<IClientTraqService>();
            _ws = _traq.CreateClientWebSocket();
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            return Task.CompletedTask;
        }

        protected virtual ValueTask OnMessageCreated(CancellationToken ct)
        {
            return ValueTask.CompletedTask;
        }

        async ValueTask StartWebSocketAsync(CancellationToken ct)
        {
            var ws = _ws;
            var baseUri = _traq.BaseUri;
            var logger = _logger;

            try
            {
                Uri wsUri = new(baseUri, "api/v3/bots/ws");
                Uri uri = baseUri.Scheme switch
                {
                    "http" => new($"ws://{wsUri.Host}{wsUri.AbsolutePath}"),
                    "https" => new($"wss://{wsUri.Host}{wsUri.AbsolutePath}"),
                    _ => throw new Exception("Unknown scheme of the baseUri.")
                };
                await ws.ConnectAsync(uri, ct);
            }
            catch(Exception e)
            {
                logger?.LogError(e, "Failed to connect to a WebSocket server.");
                throw;
            }
            logger?.LogInformation("Successfully connected to a WebSocket server.");
        }

        public override async Task StartAsync(CancellationToken ct)
        {
            await base.StartAsync(ct);
            await StartWebSocketAsync(ct);
            return;
        }

        public override async Task StopAsync(CancellationToken ct)
        {
            await base.StopAsync(ct);

            if (_ws.State == WebSocketState.Open)
            {
                await _ws.CloseAsync(WebSocketCloseStatus.NormalClosure, null, ct);
            }
        }
    }
}
