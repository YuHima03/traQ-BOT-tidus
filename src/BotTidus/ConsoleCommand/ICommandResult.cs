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

    static class CommandErrors
    {
        public const string UnknownCommand = "Unknown command";

        public const string InvalidArguments = "Invalid arguments";

        public const string InternalError = "Internal error";

        public const string PermissionDenied = "Permission denied";

        public const string Unknown = "Unknown error";
    }
}
