using BotTidus.Configurations;
using BotTidus.ConsoleCommand;
using BotTidus.Domain;
using BotTidus.Domain.MessageFaceScores;
using BotTidus.Helpers;
using BotTidus.Services.FaceCollector;
using Microsoft.Extensions.Caching.Memory;
using System.Text;
using Traq;
using Traq.Bot.Models;

namespace BotTidus.Services.InteractiveBot.CommandHandlers
{
    /*
     * face {-h|--help}
     * face cancel <MESSAGE>
     * face count [{-u|--user} <USER>]
     * face rank [-b|--include-bots] [-i|--inverse] [{-a|--all}|{-t|--take} <COUNT>]
     * face update {phrase|reaction} <MESSAGE> [--add <COUNT>] [--sub <COUNT>]
     */
    struct FaceCommandHandler(TraqBotOptions botOptions, IMemoryCache cache, BotEventUser sender, IRepositoryFactory repoFactory, ITraqApiClient traq) : IAsyncConsoleCommandHandler<FaceCommandResult>
    {
        bool _help = false;
        string? _messageIdOrUri;
        SubCommands? _subCommand;
        string? _username;

        bool _rank_all = false;
        bool _rank_includeDeactivatedUsers = false;
        bool _rank_includeBots = false;
        bool _rank_inverse = false;
        int? _rank_take = null;

        UpdateRecordTypes _update_recordType;
        uint? _update_add = null;
        uint? _update_sub = null;

        static readonly int RankTakeDefault = 10;

        public readonly bool RequiredArgumentsAreFilled => _subCommand is not null;

        public async ValueTask<FaceCommandResult> ExecuteAsync(CancellationToken cancellationToken)
        {
            await using var repo = await repoFactory.CreateRepositoryAsync(cancellationToken);

            if (_help)
            {
                return new()
                {
                    IsSuccessful = true,
                    Message = """
                        ```plain
                        Usage[0]: /face {-h|--help}

                            Displays this help message.

                        Usage[0]: /face cancel <MESSAGE>

                            Cancels the face count of the specified message.
                            This command requires permission.

                        Arguments:
                            <MESSAGE>  The id or uri of the message to cancel the face count of.

                        Usage[1]: /face count [{-u|--user} <USER>]

                            Displays the face count of the specified user.
                            If no user is specified, the face count of the sender is displayed.

                        Arguments:
                            <USER>  The name of the user to display the face count of.

                        Usage[2]: /face rank [OPTIONS]

                            Displays the face ranking of all users.

                        Options:
                            -b, --include-bots               Includes bots in the ranking.
                            -d, --include-deactivated-users  Includes deactivated users in the ranking.
                            -i, --inv                        Displays the ranking in reverse order.
                        ```
                        """
                };
            }
            else if (_subCommand is null)
            {
                return new() { IsSuccessful = false, ErrorType = CommandErrorType.InvalidArguments };
            }

            try
            {
                switch (_subCommand)
                {
                    case SubCommands.CancelMessageFaceCount:
                    {
                        if (sender.Id != botOptions.AdminUserId)
                        {
                            return new() { IsSuccessful = false, ErrorType = CommandErrorType.PermissionDenied };
                        }
                        if (_messageIdOrUri is null)
                        {
                            return new() { IsSuccessful = false, ErrorType = CommandErrorType.InvalidArguments, Message = "Message id or uri is required." };
                        }

                        Guid messageId;
                        if (!Guid.TryParse(_messageIdOrUri, out messageId))
                        {
                            if (!Uri.TryCreate(_messageIdOrUri, UriKind.Absolute, out var uri)
                                || !Guid.TryParse(uri.AbsolutePath.Split('/').LastOrDefault(), out messageId))
                            {
                                return new() { IsSuccessful = false, ErrorType = CommandErrorType.InvalidArguments, Message = $"Invalid message uri: {_messageIdOrUri}" };
                            }
                        }
                        await Task.WhenAll(
                            repo.DeleteMessageFaceScoreAsync(messageId, cancellationToken).AsTask(),
                            traq.StampApi.RemoveMessageStampAsync(messageId, MessageFaceCounter.PositiveReactionGuid, cancellationToken),
                            traq.StampApi.RemoveMessageStampAsync(messageId, MessageFaceCounter.NegativeReactionGuid, cancellationToken)
                            );
                        return new() { IsSuccessful = true, ReactionStampId = Constants.TraqStamps.WhiteCheckMark.Id };
                    }
                    case SubCommands.DisplayCount:
                    {
                        string username = sender.Name;
                        Guid userId = sender.Id;
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
                            else if (await traq.UserApi.TryGetCachedUserIdAsync(_username, cache, out var userTask, cancellationToken))
                            {
                                username = _username;
                                userId = await userTask;
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

                        if (_rank_inverse)
                        {
                            Array.Sort(faceCounts, static (a, b) => a.TotalScore - b.TotalScore);
                        }
                        else
                        {
                            Array.Sort(faceCounts, static (a, b) => b.TotalScore - a.TotalScore);
                        }

                        StringBuilder sb = new("""
                        顔ランキング
                        | 順位 | ユーザー | 現在の数 |
                        | ---: | :------ | -------: |
                        """);
                        sb.AppendLine();


                        var filteredFaceCounts = faceCounts.ToAsyncEnumerable();
                        var cache_ = cache;
                        if (!_rank_includeBots)
                        {
                            var traq_ = traq;
                            filteredFaceCounts = filteredFaceCounts.WhereAwaitWithCancellation(async (x, ct) => !(await traq_.UserApi.GetCachedUserAbstractAsync(x.UserId, cache_, ct)).IsBot);
                        }
                        if (!_rank_includeDeactivatedUsers)
                        {
                            var traq_ = traq;
                            filteredFaceCounts = filteredFaceCounts.WhereAwaitWithCancellation(async (x, ct) => (await traq_.UserApi.GetCachedUserAsync(x.UserId, cache_, ct)).State != Traq.Model.UserAccountState.deactivated);
                        }
                        if (!_rank_all)
                        {
                            var takeCount = _rank_take ?? RankTakeDefault;
                            filteredFaceCounts = filteredFaceCounts.TakeWhile((_, i) => i < takeCount);
                        }

                        int rank = 1;
                        int prevCount = int.MinValue;

                        await using var en = filteredFaceCounts.GetAsyncEnumerator(cancellationToken);
                        while (await en.MoveNextAsync(cancellationToken))
                        {
                            var current = en.Current;
                            var username = await traq.UserApi.GetCachedUserNameAsync(current.UserId, cache, cancellationToken);
                            var count = current.TotalScore;
                            sb.AppendLine($"| {(count == prevCount ? "-" : rank)} | :@{username}: {username} | {count} |");
                            prevCount = count;
                            rank++;
                        }

                        return new() { IsSuccessful = true, Message = sb.ToString() };
                    }
                    case SubCommands.UpdateFaceCount:
                    {
                        if (sender.Id != botOptions.AdminUserId)
                        {
                            return new() { IsSuccessful = false, ErrorType = CommandErrorType.PermissionDenied };
                        }
                        if (_messageIdOrUri is null)
                        {
                            return new() { IsSuccessful = false, ErrorType = CommandErrorType.InvalidArguments, Message = "Message id or uri is required." };
                        }

                        Guid messageId;
                        if (!Guid.TryParse(_messageIdOrUri, out messageId))
                        {
                            if (!Uri.TryCreate(_messageIdOrUri, UriKind.Absolute, out var uri)
                                || !Guid.TryParse(uri.AbsolutePath.Split('/').LastOrDefault(), out messageId))
                            {
                                return new() { IsSuccessful = false, ErrorType = CommandErrorType.InvalidArguments, Message = $"Invalid message uri: {_messageIdOrUri}" };
                            }
                        }

                        (uint add, uint sub) = (_update_add ?? 0, _update_sub ?? 0);
                        if (add == 0 && sub == 0)
                        {
                            // remove record
                            await Task.WhenAll(
                                repo.DeleteMessageFaceScoreAsync(messageId, cancellationToken).AsTask(),
                                traq.StampApi.RemoveMessageStampAsync(messageId, MessageFaceCounter.PositiveReactionGuid, cancellationToken),
                                traq.StampApi.RemoveMessageStampAsync(messageId, MessageFaceCounter.NegativeReactionGuid, cancellationToken)
                                );
                            return new() { IsSuccessful = true, ReactionStampId = Constants.TraqStamps.WhiteCheckMark.Id };
                        }

                        var recordType = _update_recordType;
                        if (recordType != UpdateRecordTypes.Phrase && recordType != UpdateRecordTypes.Reaction)
                        {
                            return new() { IsSuccessful = false, ErrorType = CommandErrorType.InvalidArguments, Message = "Invalid record type." };
                        }

                        var traq_ = traq;
                        var score = await repo.AddOrUpdateMessageFaceScoreAsync(messageId, async (value, ct) =>
                        {
                            if (value is null)
                            {
                                var msgDetail = await traq_.MessageApi.GetMessageAsync(messageId, cancellationToken);
                                return recordType switch
                                {
                                    UpdateRecordTypes.Phrase => new MessageFaceScore(messageId, msgDetail.UserId, 0, 0, 0, 0) with { NegativePhraseCount = sub, PositivePhraseCount = add },
                                    UpdateRecordTypes.Reaction => new MessageFaceScore(messageId, msgDetail.UserId, 0, 0, 0, 0) with { NegativeReactionCount = sub, PositiveReactionCount = add },
                                    _ => null!
                                };
                            }
                            else
                            {
                                return recordType switch
                                {
                                    UpdateRecordTypes.Phrase => value with { NegativePhraseCount = sub, PositivePhraseCount = add },
                                    UpdateRecordTypes.Reaction => value with { NegativeReactionCount = sub, PositiveReactionCount = add },
                                    _ => null!
                                };
                            }
                        },
                        cancellationToken);

                        await Task.WhenAll(
                            traq.StampApi.RemoveMessageStampAsync(messageId, MessageFaceCounter.PositiveReactionGuid, cancellationToken),
                            traq.StampApi.RemoveMessageStampAsync(messageId, MessageFaceCounter.NegativeReactionGuid, cancellationToken)
                            );
                        await Task.WhenAll(
                            traq.StampApi.AddManyMessageStampAsync(messageId, MessageFaceCounter.PositiveReactionGuid, (int)(score.PositivePhraseCount + score.PositiveReactionCount), cancellationToken).AsTask(),
                            traq.StampApi.AddManyMessageStampAsync(messageId, MessageFaceCounter.NegativeReactionGuid, (int)(score.NegativePhraseCount + score.NegativeReactionCount), cancellationToken).AsTask()
                            );

                        return new() { IsSuccessful = true, ReactionStampId = Constants.TraqStamps.WhiteCheckMark.Id };
                    }
                }
            }
            catch (Exception ex)
            {
                return new() { IsSuccessful = false, ErrorType = CommandErrorType.InternalError, Message = ex.Message };
            }
            return new() { IsSuccessful = false, ErrorType = CommandErrorType.InvalidArguments };
        }

        public bool TryReadArguments(ConsoleCommandReader reader)
        {
            if (reader.NextArgumentNameOnly(out var argName))
            {
                if (argName is "-h" or "--help")
                {
                    _help = true;
                    return reader.EnumeratedAll;
                }
                return false;
            }

            if (!reader.NextValueOnly(out var subcommand))
            {
                return false;
            }
            _subCommand = subcommand switch
            {
                "cancel" => SubCommands.CancelMessageFaceCount,
                "count" => SubCommands.DisplayCount,
                "rank" => SubCommands.DisplayRanking,
                "update" => SubCommands.UpdateFaceCount,
                _ => SubCommands.Unknown
            };

            if (_subCommand == SubCommands.CancelMessageFaceCount)
            {
                if (!reader.NextValueOnly(out var message))
                {
                    return false;
                }
                _messageIdOrUri = message.ToString();
            }
            else if (_subCommand == SubCommands.DisplayCount)
            {
                if (reader.NextNamedArgument(out var arg))
                {
                    if (arg.Name is "-u" or "--user")
                    {
                        if (_username is not null)
                        {
                            return false;
                        }
                        _username = arg.Value.ToString();
                    }
                }
            }
            else if (_subCommand == SubCommands.DisplayRanking)
            {
                while (reader.NextArgument(out var arg))
                {
                    if (arg.HasValue)
                    {
                        if (arg.Name is "-t" or "--take")
                        {
                            if (_rank_all)
                            {
                                // The arguments (-t|--take) and (-a|--all) cannot be specified at the same time.
                                return false;
                            }
                            if (!int.TryParse(arg.Value, out var cnt))
                            {
                                return false;
                            }
                            _rank_take = cnt;
                        }
                        else
                        {
                            return false;
                        }
                    }
                    else
                    {
                        switch (arg.Name)
                        {
                            case "-a":
                            case "--a":
                            {
                                if (_rank_take is not null)
                                {
                                    // The arguments (-t|--take) and (-a|--all) cannot be specified at the same time.
                                    return false;
                                }
                                _rank_all = true;
                                break;
                            }

                            case "-b":
                            case "--include-bots":
                                _rank_includeBots = true;
                                break;

                            case "-d":
                            case "--include-deactivated-users":
                                _rank_includeDeactivatedUsers = true;
                                break;

                            case "-i":
                            case "--inv":
                                _rank_inverse = true;
                                break;
                        }
                    }
                }
            }
            else if (_subCommand == SubCommands.UpdateFaceCount)
            {
                if (!reader.NextValueOnly(out var recordType) || !reader.NextValueOnly(out var msg))
                {
                    return false;
                }
                _messageIdOrUri = msg.ToString();

                switch (recordType)
                {
                    case "phrase":
                        _update_recordType = UpdateRecordTypes.Phrase;
                        break;
                    case "reaction":
                        _update_recordType = UpdateRecordTypes.Reaction;
                        break;
                    default:
                        return false;
                }

                while (reader.NextNamedArgument(out var arg))
                {
                    if (!uint.TryParse(arg.Value, out var cnt))
                    {
                        return false;
                    }

                    switch (arg.Name)
                    {
                        case "--add":
                            if (_update_add is not null)
                            {
                                return false;
                            }
                            _update_add = cnt;
                            break;

                        case "--sub":
                            if (_update_sub is not null)
                            {
                                return false;
                            }
                            _update_sub = cnt;
                            break;
                    }
                }
            }

            return reader.EnumeratedAll;
        }

        enum SubCommands : byte
        {
            Unknown = 0,
            DisplayCount,
            DisplayRanking,
            CancelMessageFaceCount,
            UpdateFaceCount,
        }

        enum UpdateRecordTypes : byte
        {
            Phrase, Reaction
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
