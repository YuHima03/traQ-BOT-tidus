namespace BotTidus.ConsoleCommand
{
    internal static class CommandHandler
    {
        public static bool TryExecuteCommand<THandler, TResult>(THandler handler, ref ConsoleCommandReader reader, out ValueTask<TResult> resultTask, CancellationToken ct)
            where THandler : IAsyncConsoleCommandHandler<TResult>, allows ref struct
            where TResult : struct, ICommandResult
        {
            if (!handler.TryReadArguments(reader))
            {
                resultTask = ValueTask.FromResult(default(TResult));
                return false;
            }
            resultTask = handler.ExecuteAsync(ct);
            return true;
        }
    }
}
