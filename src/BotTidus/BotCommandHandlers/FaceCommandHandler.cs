﻿using BotTidus.ConsoleCommand;
using BotTidus.Domain;
using BotTidus.Domain.MessageFaceScores;
using BotTidus.Helpers;
using System.Text;
using Traq;
using Traq.Bot.Models;

namespace BotTidus.BotCommandHandlers
{
    struct FaceCommandHandler(BotEventUser sender, IRepositoryFactory repoFactory, ITraqApiClient traq) : IAsyncConsoleCommandHandler<FaceCommandResult>
    {
        IRepositoryFactory _repoFactory = repoFactory;
        BotEventUser _sender = sender;
        ITraqApiClient _traq = traq;

        SubCommands? _subCommand;
        string? _username;

        public readonly bool RequiredArgumentsAreFilled => _subCommand is not null;

        public async ValueTask<FaceCommandResult> ExecuteAsync(CancellationToken cancellationToken)
        {
            IMessageFaceScoresRepository repo = await _repoFactory.CreateRepositoryAsync(cancellationToken);

            if (_subCommand is null)
            {
                return new() { IsSuccessful = false, ErrorType = CommandErrorType.InvalidArguments };
            }
            switch (_subCommand)
            {
                case SubCommands.DisplayCount:
                {
                    string username = _sender.Name;
                    Guid userId = _sender.Id;
                    if (_username is not null)
                    {
                        if (Traq.Extensions.Messages.Embedding.TryParseHead(_username.AsSpan(), out var embedding) == _username.Length)
                        {
                            if (embedding.Type != Traq.Extensions.Messages.EmbeddingType.UserMention)
                            {
                                return new() { IsSuccessful = false, ErrorType = CommandErrorType.InvalidArguments, Message = "The embedding is not mentioning a user." };
                            }
                            username = embedding.DisplayText.StartsWith("@") ? embedding.DisplayText[1..].ToString() : embedding.DisplayText.ToString();
                            userId = embedding.EmbeddedId;
                        }
                        else if (await TraqHelper.TryGetUserIdFromNameAsync(_traq.UserApi, _username, out var userTask, cancellationToken))
                        {
                            username = _username;
                            userId = (await userTask).Id;
                        }
                        else
                        {
                            return new() { IsSuccessful = false, ErrorType = CommandErrorType.InternalError, Message = $"User not found: {_username}" };
                        }
                    }

                    var count = await repo.GetUserFaceCountAsync(userId, cancellationToken);
                    return new()
                    {
                        IsSuccessful = true,
                        Message = count switch
                        {
                            { NegativePhraseCount: 0, NegativeReactionCount: 0, PositivePhraseCount: 0, PositiveReactionCount: 0 } => $$"""
                            :@{{username}}: {{username}} の現在の顔: **{{count.TotalScore}}** 個
                            顔の増減はまだないようです.
                            """,
                            _ => $$"""
                            :@{{username}}: {{username}} の現在の顔: **{{count.TotalScore}}** 個
                            - :dotted_line_face: {{count.NegativePhraseCount + count.NegativeReactionCount}} 回
                            - :star_struck: {{count.PositivePhraseCount + count.PositiveReactionCount}} 回
                            """
                        }
                    };
                }
                case SubCommands.DisplayRanking:
                {
                    if (_username is not null)
                    {
                        return new() { IsSuccessful = false, ErrorType = CommandErrorType.InvalidArguments };
                    }

                    var faceCounts = await repo.GetUserFaceCountsAsync(cancellationToken);
                    if (faceCounts.Length == 0)
                    {
                        return new() { IsSuccessful = true, Message = "まだ誰も顔の増減が無いようです." };
                    }

                    Array.Sort(faceCounts, (a, b) => a.TotalScore - b.TotalScore);
                    StringBuilder sb = new("""
                        顔ランキング
                        | 順位 | ユーザー | 現在の数 |
                        | ---: | :------ | -------: |
                        """);
                    sb.AppendLine();

                    int rank = 1;
                    int prevCount = int.MinValue;
                    for (int i = 0; i < faceCounts.Length; i++)
                    {
                        var current = faceCounts[i];
                        var user = await _traq.UserApi.GetUserAsync(current.UserId, cancellationToken);
                        var count = current.TotalScore;
                        sb.AppendLine($"| {(count == prevCount ? null : rank)} | :@{user.Name}: {user.Name} | {count} |");
                        prevCount = count;
                        rank++;
                    }

                    return new() { IsSuccessful = true, Message = sb.ToString() };
                }
            }
            return new() { IsSuccessful = false, ErrorType = CommandErrorType.InvalidArguments };
        }

        public bool TryReadArguments(ConsoleCommandReader reader)
        {
            if (!reader.NextValueOnly(out var subcommand))
            {
                return false;
            }
            _subCommand = subcommand switch
            {
                "count" => SubCommands.DisplayCount,
                "rank" => SubCommands.DisplayRanking,
                _ => SubCommands.Unknown
            };

            while (reader.NextArgument(out var arg))
            {
                if (arg.HasName && arg.HasValue)
                {
                    switch (arg.Name)
                    {
                        case "--user":
                        case "-u":
                            if (_username is not null)
                            {
                                return false;
                            }
                            _username = arg.Value.ToString();
                            return true;
                    }
                }
                return false;
            }
            return true;
        }

        enum SubCommands : byte
        {
            Unknown = 0,
            DisplayCount,
            DisplayRanking
        }
    }

    readonly struct FaceCommandResult : ICommandResult
    {
        public CommandErrorType ErrorType { get; init; }
        public bool IsSuccessful { get; init; }
        public string? Message { get; init; }
        public Guid? ReactionStampId { get; init; }

        public override string? ToString() => Message;
    }
}
