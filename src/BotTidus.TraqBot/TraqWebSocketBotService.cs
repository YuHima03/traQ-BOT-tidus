using BotTidus.TraqBot.Models;
using BotTidus.TraqClient;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Buffers;
using System.Net.WebSockets;
using System.Text.Json;

namespace BotTidus.TraqBot
{
    public class TraqWebSocketBotService : BackgroundService
    {
        const int WebSocketBufferSize = 1 << 16;

        readonly ILogger<TraqWebSocketBotService>? _logger;
        readonly IClientTraqService _traq;
        readonly ClientWebSocket _ws;
        readonly byte[] _wsBuffer;

        public TraqWebSocketBotService(IServiceProvider provider)
        {
            _logger = provider.GetService<ILogger<TraqWebSocketBotService>>();
            _traq = provider.GetRequiredService<IClientTraqService>();
            _ws = _traq.CreateClientWebSocket();
            _wsBuffer = ArrayPool<byte>.Shared.Rent(WebSocketBufferSize);
        }

        public override void Dispose()
        {
            base.Dispose();

            _ws?.Dispose();
            ArrayPool<byte>.Shared.Return(_wsBuffer);

            GC.SuppressFinalize(this);
        }

        protected sealed override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.WhenAll(
                    HandleMessageAsync(stoppingToken),
                    Task.Delay(TimeSpan.FromMilliseconds(500), stoppingToken)
                );
            }
        }

        async Task HandleMessageAsync(CancellationToken ct)
        {
            ArraySegment<byte> buffer = new(_wsBuffer);
            var logger = _logger;
            var ws = _ws;

            var result = await ws.ReceiveAsync(buffer, ct);
            if (result.MessageType == WebSocketMessageType.Close)
            {
                logger?.LogWarning("Received a close message: {}", result.CloseStatusDescription);
                await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, null, ct);
                await StartWebSocketAsync(ct);
                return;
            }
            else if (result.MessageType == WebSocketMessageType.Binary)
            {
                logger?.LogError("Received a binary message.");
                await ws.CloseAsync(WebSocketCloseStatus.InvalidMessageType, "Binary message is not supported.", ct);
                await StartWebSocketAsync(ct);
                return;
            }
            else if (!result.EndOfMessage)
            {
                logger?.LogError("Received too long message: {} bytes.", result.Count);
                await ws.CloseAsync(WebSocketCloseStatus.MessageTooBig, null, ct);
                await StartWebSocketAsync(ct);
                return;
            }

            Utf8JsonReader reader = new(buffer.AsSpan()[..result.Count]);
            using var doc = JsonDocument.ParseValue(ref reader);

            var jsonRoot = doc.RootElement;
            var eventType = jsonRoot.GetProperty("type").GetString();
            var body = jsonRoot.GetProperty("body");

            if (eventType == "ERROR")
            {
                logger?.LogWarning("Received error message: {}", body.GetRawText());
                return;
            }

            var requestId = jsonRoot.GetProperty("reqId").GetString();

            switch (eventType)
            {
                case "MESSAGE_CREATED":
                {
                    var eventArgs = JsonSerializer.Deserialize<MessageCreatedEventArgs>(body) ?? throw new Exception("Deserialized value is null.");
                    await OnMessageCreated(eventArgs, ct);
                    return;
                }
            }
        }

        protected virtual ValueTask OnMessageCreated(MessageCreatedEventArgs args, CancellationToken ct)
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
            catch (Exception e)
            {
                logger?.LogError(e, "Failed to connect to a WebSocket server.");
                throw;
            }
            logger?.LogInformation("Successfully connected to a WebSocket server.");
        }

        public override async Task StartAsync(CancellationToken ct)
        {
            await StartWebSocketAsync(ct);
            await base.StartAsync(ct);
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
