namespace BotTidus.Domain.MessageFaceScores
{
    public sealed record class MessageFaceScore(
        Guid MessageId,
        Guid AuthorId,
        uint NegativePhraseCount,
        uint NegativeReactionCount,
        uint PositivePhraseCount,
        uint PositiveReactionCount
        );
}
