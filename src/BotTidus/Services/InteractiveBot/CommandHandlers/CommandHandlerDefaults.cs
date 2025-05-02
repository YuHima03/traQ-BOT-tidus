using BotTidus.ConsoleCommand;

namespace BotTidus.Services.InteractiveBot.CommandHandlers
{
    readonly struct DefaultCommandArguments { }

    readonly struct DefaultCommandResult : ICommandResult
    {
        public string? Error { get; init; }

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
                return $"Error({Error}): {Message}";
            }
        }

        public static DefaultCommandResult CreateSuccessful(string? message)
        {
            return new DefaultCommandResult
            {
                IsSuccessful = true,
                Message = message
            };
        }

        public static DefaultCommandResult CreateFailed(string error, string? message)
        {
            return new DefaultCommandResult
            {
                IsSuccessful = false,
                Error = error,
                Message = message
            };
        }
    }
}
