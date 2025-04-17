using BotTidus.ConsoleCommand;
using BotTidus.Domain;
using BotTidus.Services.InteractiveBot.CommandHandlers;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Diagnostics;
using Traq;
using Traq.Bot.Models;

namespace BotTidus.Services.InteractiveBot
{
    sealed class InteractiveBotService(ITraqApiClient traq, IServiceProvider provider) : Traq.Bot.WebSocket.TraqWsBot(traq, provider)
    {
        readonly AppConfig _appConf = provider.GetRequiredService<IOptions<AppConfig>>().Value;
        readonly IMemoryCache _cache = provider.GetRequiredService<IMemoryCache>();
        readonly HealthCheckService _healthCheckService = provider.GetRequiredService<HealthCheckService>();
        readonly ILogger<InteractiveBotService> _logger = provider.GetRequiredService<ILogger<InteractiveBotService>>();
        readonly IRepositoryFactory _repoFactory = provider.GetRequiredService<IRepositoryFactory>();
        readonly ITraqApiClient _traq = traq;

        public static readonly Guid StampId_Explosion = new("27475336-812d-4040-9c0e-c7367cd1c966");        // explosion
        public static readonly Guid StampId_Question = new("408b504e-89c1-474b-abfb-16779a3ee595");         // question
        public static readonly Guid StampId_Success = new("93d376c3-80c9-4bb2-909b-2bbe2fbf9e93");          // white_check_mark
        public static readonly Guid StampId_PermissionDenied = new("544c04db-9cc3-4c0e-935d-571d4cf103a2"); // no_entry_sign

        protected override ValueTask OnDirectMessageCreatedAsync(MessageCreatedOrUpdatedEventArgs args, CancellationToken ct)
        {
            return OnMessageCreatedAsync(args, ct);
        }

        protected override async ValueTask OnMessageCreatedAsync(MessageCreatedOrUpdatedEventArgs args, CancellationToken ct)
        {
            _logger.LogDebug("Received message: {Message}", args.Message.Text);

            var stopwatch = Stopwatch.StartNew();

            if (await TryHandleAsCommandAsync(args.Message, ct))
            {
                stopwatch.Stop();
                _logger.LogInformation("Executed command [{ElapsedMilliseconds}ms]: {Command}", stopwatch.ElapsedMilliseconds, args.Message.Text);
                return;
            }
        }

        async ValueTask<bool> TryHandleAsCommandAsync(BotEventMessage message, CancellationToken ct)
        {
            var commandText = message.Text.AsSpan().Trim();
            if (commandText.IsEmpty)
            {
                return false;
            }

            bool isStartWithMention = false;
            var leadingEmbeddingExpLength = Traq.Extensions.Messages.Embedding.TryParseHead(commandText, out var embedding);
            if (leadingEmbeddingExpLength != 0 && embedding.Type == Traq.Extensions.Messages.EmbeddingType.UserMention && embedding.EmbeddedId == _appConf.BotUserId)
            {
                commandText = commandText[leadingEmbeddingExpLength..].TrimStart();
                isStartWithMention = true;
            }

            if (!ConsoleCommandReader.TryCreate(commandText, isStartWithMention, _appConf.BotCommandPrefix.AsSpan(), out var reader))
            {
                return false;
            }

            switch (reader.CommandName)
            {
                case "face":
                {
                    if (CommandHandler.TryExecuteCommand<FaceCommandHandler, FaceCommandResult>(new(_appConf, _cache, message.Author, _repoFactory, _traq), ref reader, out var resultTask, ct))
                    {
                        var result = await resultTask;
                        if (result.IsSuccessful)
                        {
                            if (!string.IsNullOrWhiteSpace(result.Message))
                            {
                                await _traq.MessageApi.PostMessageAsync(message.ChannelId, new Traq.Model.PostMessageRequest(result.Message, false), ct);
                            }
                            else if (result.ReactionStampId is not null)
                            {
                                await _traq.MessageApi.AddMessageStampAsync(message.Id, result.ReactionStampId.Value, new Traq.Model.PostMessageStampRequest(1), ct);
                            }
                            return true;
                        }
                    }
                    await HandleCommandError(message, await resultTask, ct);
                    return false;
                }
                case "hello":
                {
                    if (CommandHandler.TryExecuteCommand<HelloCommandHandler, HelloCommandResult>(new(message.Author), ref reader, out var resultTask, ct))
                    {
                        var result = await resultTask;
                        if (result.IsSuccessful && !string.IsNullOrWhiteSpace(result.Message))
                        {
                            await _traq.MessageApi.PostMessageAsync(message.ChannelId, new Traq.Model.PostMessageRequest(result.Message, false), ct);
                            return true;
                        }
                    }
                    await HandleCommandError(message, await resultTask, ct);
                    return false;
                }
                case "help":
                {
                    if (CommandHandler.TryExecuteCommand<HelpCommandHandler, HelpCommandResult>(new(), ref reader, out var resultTask, ct))
                    {
                        var result = await resultTask;
                        if (result.IsSuccessful && !string.IsNullOrWhiteSpace(result.Message))
                        {
                            await _traq.MessageApi.PostMessageAsync(message.ChannelId, new Traq.Model.PostMessageRequest(result.Message, false), ct);
                            return true;
                        }
                    }
                    await HandleCommandError(message, await resultTask, ct);
                    return false;
                }
                case "status":
                {
                    if (CommandHandler.TryExecuteCommand<StatusCommandHandler, StatusCommandResult>(new(_healthCheckService), ref reader, out var resultTask, ct))
                    {
                        var result = await resultTask;
                        if (result.IsSuccessful && !string.IsNullOrWhiteSpace(result.Message))
                        {
                            await _traq.MessageApi.PostMessageAsync(message.ChannelId, new Traq.Model.PostMessageRequest(result.Message, false), ct);
                            return true;
                        }
                    }
                    await HandleCommandError(message, await resultTask, ct);
                    return false;
                }
            }

            // Unknown command
            if (isStartWithMention)
            {
                await HandleCommandError<CommonCommandResult>(message, new() { IsSuccessful = false, ErrorType = CommandErrorType.UnknownCommand }, ct);
            }
            return false;
        }

        async ValueTask HandleCommandError<TCommandResult>(BotEventMessage message, TCommandResult result, CancellationToken ct) where TCommandResult : ICommandResult
        {
            switch (result.ErrorType)
            {
                case CommandErrorType.InternalError:
                {
                    _logger.LogWarning("Internal error occurred while executing command: {CommandText} -> {Result}", message.Text, result.ToString());
                    await _traq.MessageApi.AddMessageStampAsync(message.Id, StampId_Explosion, new Traq.Model.PostMessageStampRequest(1), ct);
                    break;
                }
                case CommandErrorType.PermissionDenied:
                {
                    _logger.LogInformation("Permission denied: {CommandText} -> {Result}", message.Text, result.ToString());
                    await _traq.MessageApi.AddMessageStampAsync(message.Id, StampId_PermissionDenied, new Traq.Model.PostMessageStampRequest(1), ct);
                    break;
                }
                default:
                {
                    _logger.LogDebug("An error occurred: {CommandText} -> {Result}", message.Text, result.ToString());
                    await _traq.MessageApi.AddMessageStampAsync(message.Id, StampId_Question, new Traq.Model.PostMessageStampRequest(1), ct);
                    break;
                }
            }
        }
    }

    readonly struct CommonCommandResult : ICommandResult
    {
        public bool IsSuccessful { get; init; }
        public CommandErrorType ErrorType { get; init; }
    }
}
