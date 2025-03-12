namespace BotTidus.Domain.MessageFaceScores
{
    public sealed record class UserFaceCount(
        Guid UserId,
        uint NegativePhraseCount,
        uint NegativeReactionCount,
        uint PositivePhraseCount,
        uint PositiveReactionCount
        )
    {
        public int TotalScore => 1 + (int)(PositivePhraseCount + PositiveReactionCount) - (int)(NegativePhraseCount + NegativeReactionCount);
    }
}
