using BotTidus.ConsoleCommand;
using BotTidus.Domain;
using BotTidus.Services.InteractiveBot.CommandHandlers;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Traq;
using Traq.Bot.Models;

namespace BotTidus.Services.InteractiveBot
{
    sealed class InteractiveBotService(IOptions<AppConfig> appConf, IMemoryCache cache, ILogger<InteractiveBotService> logger, IRepositoryFactory repoFactory, ITraqApiClient traq, IServiceProvider provider) : Traq.Bot.WebSocket.TraqWsBot(traq, provider)
    {
        readonly AppConfig _appConf = appConf.Value;
        readonly IMemoryCache _cache = cache;
        readonly ILogger<InteractiveBotService> _logger = logger;
        readonly IRepositoryFactory _repoFactory = repoFactory;
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

            if (await TryHandleAsCommandAsync(args.Message, ct))
            {
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

            bool isStartWithMention;
            if (isStartWithMention = commandText[0] == '@' && commandText[1..].StartsWith(_appConf.BotName, StringComparison.OrdinalIgnoreCase))
            {
                commandText = commandText[(_appConf.BotName.Length + 1)..];
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
                        _logger.LogDebug("Permission denied: {CommandText} -> {Result}", message.Text, result.ToString());
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
