using BotTidus.ConsoleCommand;

namespace BotTidus.Services.InteractiveBot.CommandHandlers
{
    readonly struct HelpCommandHandler : IAsyncConsoleCommandHandler<HelpCommandResult>
    {
        static readonly string HelpText = """
            Usage[0]: /<COMMAND>
                
                Execute a command.
                Type `/<COMMAND> --help` to display help of a command.

            Commands:
                hello  Greeting.
                help   Displays this help message.
            """;

        public bool RequiredArgumentsAreFilled => true;

        public ValueTask<HelpCommandResult> ExecuteAsync(CancellationToken cancellationToken)
        {
            return ValueTask.FromResult(new HelpCommandResult()
            {
                IsSuccessful = true,
                Message = $"""
                ```plain
                {HelpText}
                ```
                """
            });
        }

        public bool TryReadArguments(ConsoleCommandReader reader)
        {
            return reader.EnumeratedAll;
        }
    }

    readonly struct HelpCommandResult : ICommandResult
    {
        public bool IsSuccessful { get; init; }

        public string? Message { get; init; }

        public CommandErrorType ErrorType { get; init; }

        public override string? ToString() => Message;
    }
}
