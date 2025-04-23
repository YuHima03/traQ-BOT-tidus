using User = (System.Guid Id, string Name);

namespace BotTidus.Constants
{
    static class TraqUsers
    {
        /// <summary>
        /// <c>@tidus</c>: the admin of the bot.
        /// </summary>
        public static readonly User Tidus = (Guid.Parse("ee7d1608-6ace-46f5-835d-6a894d693af9"), "tidus");
    }
}
