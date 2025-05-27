using Microsoft.Extensions.Configuration;

namespace BotTidus
{
    class AppConfig
    {
        [ConfigurationKeyName("ADMIN_USER_ID")]
        public Guid AdminUserId { get; set; }

        [ConfigurationKeyName("BOT_COMMAND_PREFIX")]
        public string BotCommandPrefix { get; set; } = string.Empty;

        [ConfigurationKeyName("BOT_NAME")]
        public string BotName { get; set; } = string.Empty;

        [ConfigurationKeyName("BOT_ID")]
        public Guid BotId { get; set; }

        [ConfigurationKeyName("BOT_USER_ID")]
        public Guid BotUserId { get; set; }

        [ConfigurationKeyName("HEALTH_ALERT_CHANNEL_ID")]
        public Guid HealthAlertChannelId { get; set; }

        [ConfigurationKeyName("STAMP_RANKING_CHANNEL_ID")]
        public Guid StampRankingChannelId { get; set; }

        [ConfigurationKeyName("WAKARU_MESSAGE_RANKING_CHANNEL_ID")]
        public Guid WakaruMessageRankingChannelId { get; set; }
    }
}
