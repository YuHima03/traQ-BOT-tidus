namespace BotTidus.ConsoleCommand;

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
    bool IsSuccessful { get; }

    CommandErrorType ErrorType { get; }
}
