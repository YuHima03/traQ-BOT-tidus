using BotTidus.BotCommandHandlers;
using BotTidus.ConsoleCommand;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Traq;
using Traq.Bot.Models;

namespace BotTidus.Services
{
    sealed class InteractiveBotService(IOptions<AppConfig> appConf, ILogger<InteractiveBotService> logger, ITraqApiClient traq, IServiceProvider provider) : Traq.Bot.WebSocket.TraqWsBot(traq, provider)
    {
        readonly AppConfig _appConf = appConf.Value;
        readonly ILogger<InteractiveBotService> _logger = logger;
        readonly ITraqApiClient _traq = traq;

        static readonly Guid StampId_Explosion = new("27475336-812d-4040-9c0e-c7367cd1c966"); // explosion
        static readonly Guid StampId_Question = new("408b504e-89c1-474b-abfb-16779a3ee595");  // question

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
            if (isStartWithMention = (commandText[0] == '@' && commandText[1..].StartsWith(_appConf.BotName, StringComparison.OrdinalIgnoreCase)))
            {
                commandText = commandText[(_appConf.BotName.Length + 1)..];
            }

            if (!ConsoleCommandReader.TryCreate(commandText, isStartWithMention, _appConf.BotCommandPrefix.AsSpan(), out var reader))
            {
                return false;
            }

            switch (reader.CommandName)
            {
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
