namespace BotTidus.ConsoleCommand
{
    internal interface IAsyncConsoleCommandHandler
    {
        public bool RequiredArgumentsAreFilled { get; }

        public bool TryReadArguments(ConsoleCommandReader reader);
    }

    internal interface IAsyncConsoleCommandHandler<TResult> : IAsyncConsoleCommandHandler
        where TResult : ICommandResult
    {
        public ValueTask<TResult> ExecuteAsync(CancellationToken cancellationToken);
    }
}
