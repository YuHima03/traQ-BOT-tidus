namespace BotTidus.Domain.DiscordWebhook
{
    [Flags]
    public enum MessageFilter : int
    {
        None = 0,
        UserMentioned = 1,
        GroupMentioned = 2,
        UserMessageCited = 4
    }

    public record class DiscordWebhook(
        Guid Id,
        Uri? PostUrl,
        Guid UserId,
        bool IsEnabled,
        MessageFilter NotifiesOn,
        DateTimeOffset CreatedAt,
        DateTimeOffset UpdatedAt
    );
}
