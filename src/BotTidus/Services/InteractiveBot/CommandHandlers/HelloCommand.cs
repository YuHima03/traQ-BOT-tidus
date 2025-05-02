using BotTidus.ConsoleCommand;

using TraqUser = (System.Guid Id, string Name);

namespace BotTidus.Services.InteractiveBot.CommandHandlers
{
    sealed class HelloCommand : ICommandHandler<HelloCommandArguments, DefaultCommandResult>
    {
        public static ValueTask<DefaultCommandResult> ExecuteAsync(HelloCommandArguments args, CancellationToken cancellationToken)
        {
            var mentionEmbedding = string.Empty;
            if (args.RepliesTo.HasValue)
            {
                var (id, name) = args.RepliesTo.Value;
                mentionEmbedding = $"!{{\"type\":\"user\",\"raw\":\"@{name}\",\"id\":\"{id}\"}}";
            }

            return ValueTask.FromResult(DefaultCommandResult.CreateSuccessful($$"""
                Hello! {{mentionEmbedding}}
                ```plain
                    ____        __     __  _     __              ___
                   / __ )____  / /_   / /_(_)___/ /_  _______   |__ \
                  / __  / __ \/ __/  / __/ / __  / / / / ___/   __/ /
                 / /_/ / /_/ / /_   / /_/ / /_/ / /_/ (__  )   / __/
                /_____/\____/\__/   \__/_/\__,_/\__,_/____/   /____/

                (C) 2025- tidus
                ```
                """));
        }

        public static ConsoleCommandParseResult<HelloCommandArguments> GetArguments(ConsoleCommandReader reader)
        {
            if (reader.HasAnyArguments)
            {
                return ConsoleCommandParseResult<HelloCommandArguments>.CreateFailed(CommandErrors.InvalidArguments, "No arguments expected.");
            }
            else
            {
                return ConsoleCommandParseResult<HelloCommandArguments>.CreateSuccessful(new HelloCommandArguments());
            }
        }
    }

    sealed class HelloCommandArguments
    {
        public TraqUser? RepliesTo { get; set; }
    }
}
