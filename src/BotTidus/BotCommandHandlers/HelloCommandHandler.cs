using BotTidus.ConsoleCommand;
using Traq.Bot.Models;

namespace BotTidus.BotCommandHandlers
{
    readonly struct HelloCommandHandler(BotEventUser author) : IAsyncConsoleCommandHandler<HelloCommandResult>
    {
        readonly BotEventUser _author = author;

        public bool RequiredArgumentsAreFilled => true;

        public ValueTask<HelloCommandResult> ExecuteAsync(CancellationToken cancellationToken)
        {
            return new ValueTask<HelloCommandResult>(new HelloCommandResult
            {
                IsSuccessful = true,
                Message = $$"""
                Hello! !{"type":"user","raw":"@{{_author.Name}}","id":"@{{_author.Id}}"}
                ```plain
                    ____        __     __  _     __              ___
                   / __ )____  / /_   / /_(_)___/ /_  _______   |__ \
                  / __  / __ \/ __/  / __/ / __  / / / / ___/   __/ /
                 / /_/ / /_/ / /_   / /_/ / /_/ / /_/ (__  )   / __/
                /_____/\____/\__/   \__/_/\__,_/\__,_/____/   /____/

                (C) 2025- tidus
                ```
                """
            });
        }

        public bool TryReadArguments(ConsoleCommandReader reader)
        {
            return reader.EnumeratedAll;
        }
    }

    readonly struct HelloCommandResult : ICommandResult
    {
        public bool IsSuccessful { get; init; }
        public CommandErrorType ErrorType { get; init; }
        public string? Message { get; init; }
        public override string? ToString() => Message;
    }
}
