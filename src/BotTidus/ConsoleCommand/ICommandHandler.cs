namespace BotTidus.ConsoleCommand
{
    internal interface ICommandHandler<TArgs, out TResult>
        where TResult : ICommandResult
    {
        public abstract static bool TryReadArguments(ConsoleCommandReader reader, out ConsoleCommandParseResult<TArgs> result);

        public abstract static TResult ExecuteAsync(TArgs args, CancellationToken cancellationToken);
    }
}
