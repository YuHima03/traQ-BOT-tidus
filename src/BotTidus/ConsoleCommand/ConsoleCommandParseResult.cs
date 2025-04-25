namespace BotTidus.ConsoleCommand
{
    readonly struct ConsoleCommandParseResult<TResult>
    {
        public string? Error { get; init; }

        public bool IsSuccessful { get; init; }

        public string? Message { get; init; }

        public TResult Result { get; init; }

        public static ConsoleCommandParseResult<TResult> CreateSuccessful(TResult result)
        {
            return new ConsoleCommandParseResult<TResult>
            {
                IsSuccessful = true,
                Result = result
            };
        }

        public static ConsoleCommandParseResult<TResult> CreateFailed(string error, string? message)
        {
            return new ConsoleCommandParseResult<TResult>
            {
                IsSuccessful = false,
                Error = error,
                Message = message
            };
        }
    }
}
