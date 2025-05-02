namespace BotTidus.ConsoleCommand
{
    internal interface ICommandHandler<TArgs, TResult>
        where TResult : ICommandResult
    {
        public abstract static ConsoleCommandParseResult<TArgs> GetArguments(ConsoleCommandReader reader);

        public abstract static ValueTask<TResult> ExecuteAsync(TArgs args, CancellationToken cancellationToken);
    }
}
