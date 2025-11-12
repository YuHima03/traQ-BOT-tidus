namespace BotTidus.ConsoleCommand;

interface IAsyncConsoleCommandHandler
{
    bool RequiredArgumentsAreFilled { get; }

    bool TryReadArguments(ConsoleCommandReader reader);
}

interface IAsyncConsoleCommandHandler<TResult> : IAsyncConsoleCommandHandler
    where TResult : ICommandResult
{
    ValueTask<TResult> ExecuteAsync(CancellationToken cancellationToken);
}
