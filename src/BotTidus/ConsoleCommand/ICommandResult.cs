namespace BotTidus.ConsoleCommand
{
    enum CommandErrorType : byte
    {
        None = 0,
        UnknownCommand,
        InvalidArguments,
        InternalError,
        Unknown,
    }

    interface ICommandResult
    {
        public bool IsSuccessful { get; }

        public CommandErrorType ErrorType { get; }
    }
}
