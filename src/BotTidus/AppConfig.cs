using System.Diagnostics.CodeAnalysis;

namespace BotTidus
{
    class AppConfig
    {
        public Guid AdminUserId { get; set; }

        [NotNull]
        public string? BotCommandPrefix { get; set; }

        [NotNull]
        public string? BotName { get; set; }

        public Guid BotId { get; set; }

        public Guid BotUserId { get; set; }

        public Guid StampRankingChannelId { get; set; }

        public Guid WakaruMessageRankingChannelId { get; set; }
    }
}
