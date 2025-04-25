namespace BotTidus.ConsoleCommand
{
    [Obsolete]
    enum CommandErrorType : byte
    {
        None = 0,
        UnknownCommand,
        InvalidArguments,
        InternalError,
        PermissionDenied,
        Unknown,
    }

    interface ICommandResult
    {
        public bool IsSuccessful { get; }

        [Obsolete]
        public CommandErrorType ErrorType => CommandErrorType.None;

        public string? Error => null;
    }
}
