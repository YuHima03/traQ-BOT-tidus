﻿using Microsoft.Extensions.Configuration;
using Traq;

namespace BotTidus.Configurations
{
    internal sealed class TraqBotOptions
    {
        [ConfigurationKeyName("ADMIN_USER_ID")]
        public Guid AdminUserId { get; set; }

        [ConfigurationKeyName("TRAQ_API_BASE_ADDRESS")]
        public string? TraqApiBaseAddress { get; set; }

        [ConfigurationKeyName("BOT_ACCESS_TOKEN")]
        public string? TraqAccessToken { get; set; }

        [ConfigurationKeyName("BOT_COMMAND_PREFIX")]
        public string? CommandPrefix { get; set; }

        [ConfigurationKeyName("BOT_NAME")]
        public string? Name { get; set; }

        [ConfigurationKeyName("BOT_ID")]
        public Guid Id { get; set; }

        [ConfigurationKeyName("BOT_USER_ID")]
        public Guid UserId { get; set; }

        public TraqApiClientOptions GetTraqOptions() => new()
        {
            BaseAddress = TraqApiBaseAddress ?? throw new InvalidOperationException("The base address of the traQ API is not set."),
            BearerAuthToken = TraqAccessToken
        };
    }
}
