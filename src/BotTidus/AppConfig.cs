using System.Diagnostics.CodeAnalysis;

namespace BotTidus
{
    class AppConfig
    {
        [NotNull]
        public string? BotCommandPrefix { get; set; }

        [NotNull]
        public string? BotName { get; set; }

        public Guid BotUserId { get; set; }
    }
}
