using BotTidus.Configurations;
using BotTidus.ConsoleCommand;
using Microsoft.Extensions.Options;
using Traq;

namespace BotTidus.Services.InteractiveBot.CommandHandlers
{
    /// <summary>
    /// <code>
    /// /rmmsg &lt;MESSAGE_ID&gt;
    /// </code>
    /// </summary>
    sealed class DeleteMessageCommandHandler(Guid senderId, ITraqApiClient traq, IOptions<TraqBotOptions> botOptions) : IAsyncConsoleCommandHandler<DeleteMessageCommandResult>
    {
        Guid _messageId;

        public bool RequiredArgumentsAreFilled => throw new NotImplementedException();

        public async ValueTask<DeleteMessageCommandResult> ExecuteAsync(CancellationToken cancellationToken)
        {
            try
            {
                if (senderId != botOptions.Value.AdminUserId)
                {
                    return new DeleteMessageCommandResult
                    {
                        IsSuccessful = false,
                        ErrorType = CommandErrorType.PermissionDenied
                    };
                }
                if (_messageId == Guid.Empty)
                {
                    return new DeleteMessageCommandResult
                    {
                        IsSuccessful = false,
                        Message = "Message ID is not specified.",
                        ErrorType = CommandErrorType.InvalidArguments
                    };
                }
                await traq.MessageApi.DeleteMessageAsync(_messageId, cancellationToken);
                return new DeleteMessageCommandResult { IsSuccessful = true };
            }
            catch(Exception e)
            {
                return new DeleteMessageCommandResult { IsSuccessful = false, Message = e.Message, ErrorType = CommandErrorType.InternalError };
            }
        }

        public bool TryReadArguments(ConsoleCommandReader reader)
        {
            if (!reader.NextValueOnly(out var idOrUri))
            {
                return false;
            }
            if (Guid.TryParse(idOrUri, out _messageId))
            {
                return true;
            }
            else if (Uri.TryCreate(idOrUri.ToString(), UriKind.RelativeOrAbsolute, out var uri)
                && Guid.TryParse(uri.AbsolutePath.Split('/').LastOrDefault(), out _messageId))
            {
                return true;
            }
            _messageId = Guid.Empty;
            return false;
        }
    }

    readonly struct DeleteMessageCommandResult : ICommandResult
    {
        public bool IsSuccessful { get; init; }
        public string? Message { get; init; }
        public CommandErrorType ErrorType { get; init; }
        public override string? ToString() => Message;
    }
}
