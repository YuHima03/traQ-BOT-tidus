using BotTidus.Configurations;
using BotTidus.ConsoleCommand;
using BotTidus.Domain;
using BotTidus.Services.InteractiveBot.CommandHandlers;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.ObjectPool;
using Microsoft.Extensions.Options;
using System.Diagnostics;
using Traq;
using Traq.Bot.Models;

namespace BotTidus.Services.InteractiveBot
{
    sealed class InteractiveBotService(
        ITraqApiClient traq,
        IOptions<TraqApiClientOptions> traqOptions,
        IOptions<TraqBotOptions> botOptions,
        ILoggerFactory loggers,
        IRepositoryFactory repoFactory,
        IMemoryCache cache,
        HealthCheckService healthCheckService,
        ObjectPool<Traq.Model.PostBotActionJoinRequest> postBotActionJoinRequestPool,
        ObjectPool<Traq.Model.PostBotActionLeaveRequest> postBotActionLeaveRequestPool,
        ObjectPool<Traq.Model.PostMessageRequest> postMessageRequestPool,
        ObjectPool<Traq.Model.PostMessageStampRequest> postMessageStampRequestPool
        )
        : Traq.Bot.WebSocket.TraqWsBot(
            traqOptions,
            loggers.CreateLogger<Traq.Bot.WebSocket.TraqWsBot>(),
            loggers.CreateLogger<Traq.Bot.TraqBot>()
            )
    {
        readonly TraqBotOptions _botOptions = botOptions.Value;
        readonly ILogger<InteractiveBotService> _logger = loggers.CreateLogger<InteractiveBotService>();

        protected override ValueTask InitializeAsync(CancellationToken ct)
        {
            _logger.LogInformation("Command prefix: {Prefix}", _botOptions.CommandPrefix);
            return base.InitializeAsync(ct);
        }

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
            else if (MessageReactions.TryGetReaction(args.Message.Text, args.Message.Author.Id, out var reaction))
            {
                var stampReq = postMessageStampRequestPool.Get();
                stampReq.Count = 1;

                var mesReq = postMessageRequestPool.Get();
                (mesReq.Content, mesReq.Embed) = (reaction.Message!, false);

                await Task.WhenAll(
                    reaction.Stamp is not null ? traq.StampApi.AddMessageStampAsync(args.Message.Id, reaction.Stamp.Value, stampReq, ct) : Task.CompletedTask,
                    reaction.Message is not null ? traq.MessageApi.PostMessageAsync(args.Message.ChannelId, mesReq, ct) : Task.CompletedTask
                    );

                postMessageStampRequestPool.Return(stampReq);
                postMessageRequestPool.Return(mesReq);
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
            if (leadingEmbeddingExpLength != 0 && embedding.Type == Traq.Extensions.Messages.EmbeddingType.UserMention && embedding.EmbeddedId == _botOptions.UserId)
            {
                commandText = commandText[leadingEmbeddingExpLength..].TrimStart();
                isStartWithMention = true;
            }

            if (!ConsoleCommandReader.TryCreate(commandText, isStartWithMention, _botOptions.CommandPrefix.AsSpan(), out var reader))
            {
                return false;
            }

            switch (reader.CommandName)
            {
                case "face":
                {
                    if (CommandHandler.TryExecuteCommand<FaceCommandHandler, FaceCommandResult>(new(_botOptions, cache, message.Author, repoFactory, traq), ref reader, out var resultTask, ct))
                    {
                        var result = await resultTask;
                        if (result.IsSuccessful)
                        {
                            if (!string.IsNullOrWhiteSpace(result.Message))
                            {
                                var mesReq = postMessageRequestPool.Get();
                                (mesReq.Content, mesReq.Embed) = (result.Message, false);
                                await traq.MessageApi.PostMessageAsync(message.ChannelId, mesReq, ct);
                                postMessageRequestPool.Return(mesReq);
                            }
                            else if (result.ReactionStampId is not null)
                            {
                                var stampReq = postMessageStampRequestPool.Get();
                                stampReq.Count = 1;
                                await traq.MessageApi.AddMessageStampAsync(message.Id, result.ReactionStampId.Value, stampReq, ct);
                                postMessageStampRequestPool.Return(stampReq);
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
                            var mesReq = postMessageRequestPool.Get();
                            (mesReq.Content, mesReq.Embed) = (result.Message, false);
                            await traq.MessageApi.PostMessageAsync(message.ChannelId, mesReq, ct);
                            postMessageRequestPool.Return(mesReq);
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
                            var mesReq = postMessageRequestPool.Get();
                            (mesReq.Content, mesReq.Embed) = (result.Message, false);
                            await traq.MessageApi.PostMessageAsync(message.ChannelId, mesReq, ct);
                            postMessageRequestPool.Return(mesReq);
                            return true;
                        }
                    }
                    await HandleCommandError(message, await resultTask, ct);
                    return false;
                }
                case "status":
                {
                    if (CommandHandler.TryExecuteCommand<StatusCommandHandler, StatusCommandResult>(new(healthCheckService), ref reader, out var resultTask, ct))
                    {
                        var result = await resultTask;
                        if (result.IsSuccessful && !string.IsNullOrWhiteSpace(result.Message))
                        {
                            var mesReq = postMessageRequestPool.Get();
                            (mesReq.Content, mesReq.Embed) = (result.Message, false);
                            await traq.MessageApi.PostMessageAsync(message.ChannelId, mesReq, ct);
                            postMessageRequestPool.Return(mesReq);
                            return true;
                        }
                    }
                    await HandleCommandError(message, await resultTask, ct);
                    return false;
                }

                case "join":
                {
                    if (reader.HasAnyArguments)
                    {
                        await HandleCommandError(message, CommonCommandResult.CreateFailed(CommandErrorType.InvalidArguments), ct);
                        return false;
                    }
                    if (_botOptions.Id == Guid.Empty)
                    {
                        await HandleCommandError(message, CommonCommandResult.CreateFailed(CommandErrorType.InternalError, "Bot ID is not set."), ct);
                        return false;
                    }

                    var stampReq = postMessageStampRequestPool.Get();
                    stampReq.Count = 1;

                    var joinReq = postBotActionJoinRequestPool.Get();
                    joinReq.ChannelId = message.ChannelId;

                    try
                    {
                        await traq.BotApi.LetBotJoinChannelAsync(_botOptions.Id, joinReq, ct);
                        await traq.StampApi.AddMessageStampAsync(message.Id, Constants.TraqStamps.WhiteCheckMark.Id, stampReq, ct);
                    }
                    catch (Exception e)
                    {
                        _logger.LogError(e, "Failed to join the channel.");
                        await HandleCommandError(message, CommonCommandResult.CreateFailed(CommandErrorType.InternalError), ct);
                    }

                    postMessageStampRequestPool.Return(stampReq);
                    postBotActionJoinRequestPool.Return(joinReq);
                    return true;
                }
                case "leave":
                {
                    if (reader.HasAnyArguments)
                    {
                        await HandleCommandError(message, CommonCommandResult.CreateFailed(CommandErrorType.InvalidArguments), ct);
                        return false;
                    }
                    if (_botOptions.Id == Guid.Empty)
                    {
                        await HandleCommandError(message, CommonCommandResult.CreateFailed(CommandErrorType.InternalError, "Bot ID is not set."), ct);
                        return false;
                    }

                    var stampReq = postMessageStampRequestPool.Get();
                    stampReq.Count = 1;

                    var leaveReq = postBotActionLeaveRequestPool.Get();
                    leaveReq.ChannelId = message.ChannelId;

                    try
                    {
                        await traq.BotApi.LetBotLeaveChannelAsync(_botOptions.Id, leaveReq, ct);
                        await traq.StampApi.AddMessageStampAsync(message.Id, Constants.TraqStamps.Wave.Id, stampReq, ct);
                    }
                    catch (Exception e)
                    {
                        _logger.LogError(e, "Failed to leave the channel.");
                        await HandleCommandError(message, CommonCommandResult.CreateFailed(CommandErrorType.InternalError), ct);
                    }

                    postMessageStampRequestPool.Return(stampReq);
                    postBotActionLeaveRequestPool.Return(leaveReq);
                    return true;
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
            var req = postMessageStampRequestPool.Get();
            req.Count = 1;

            switch (result.ErrorType)
            {
                case CommandErrorType.InternalError:
                {
                    _logger.LogWarning("Internal error occurred while executing command: {CommandText} -> {Result}", message.Text, result.ToString());
                    await traq.MessageApi.AddMessageStampAsync(message.Id, Constants.TraqStamps.Explosion.Id, req, ct);
                    break;
                }
                case CommandErrorType.PermissionDenied:
                {
                    _logger.LogInformation("Permission denied: {CommandText} -> {Result}", message.Text, result.ToString());
                    await traq.MessageApi.AddMessageStampAsync(message.Id, Constants.TraqStamps.NoEntrySign.Id, req, ct);
                    break;
                }
                default:
                {
                    _logger.LogDebug("An error occurred: {CommandText} -> {Result}", message.Text, result.ToString());
                    await traq.MessageApi.AddMessageStampAsync(message.Id, Constants.TraqStamps.Question.Id, req, ct);
                    break;
                }
            }
            postMessageStampRequestPool.Return(req);
        }
    }

    readonly struct CommonCommandResult : ICommandResult
    {
        public CommandErrorType ErrorType { get; init; }
        public bool IsSuccessful { get; init; }
        public string? Message { get; init; }

        public override string ToString()
        {
            if (IsSuccessful)
            {
                return Message ?? string.Empty;
            }
            else
            {
                return $"Error({ErrorType}): {Message ?? string.Empty}";
            }
        }

        public static CommonCommandResult CreateFailed(CommandErrorType errorType, string? message = null)
        {
            return new CommonCommandResult
            {
                ErrorType = errorType,
                IsSuccessful = false,
                Message = message
            };
        }
    }
}
