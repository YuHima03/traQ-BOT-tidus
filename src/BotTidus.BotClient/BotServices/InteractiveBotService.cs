using BotTidus.TraqBot;
using BotTidus.TraqBot.Models;
using BotTidus.TraqClient;
using BotTidus.TraqClient.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Runtime.InteropServices;
using Yuh.Collections;

namespace BotTidus.BotClient.BotServices
{
    internal class InteractiveBotService(IServiceProvider provider) : TraqWebSocketBotService(provider)
    {
        readonly ILogger<InteractiveBotService>? _logger = provider.GetService<ILogger<InteractiveBotService>>();

        readonly Traq _traq = provider.GetRequiredService<IClientTraqService>().Traq;

        protected override async ValueTask OnMessageCreated(MessageCreatedEventArgs args, CancellationToken ct)
        {
            _logger?.LogInformation("Received a message: {}", args.Message.PlainText);

            var channel = await _traq.PublicChannels.FindAsync(args.Message.ChannelId, ct);

            var text = args.Message.PlainText.AsSpan().Trim();
            if (text.StartsWith("@bot_tidus", StringComparison.OrdinalIgnoreCase))
            {
                text = text[10..].Trim();
            }

            Span<Range> ranges = stackalloc Range[32];
            var sectionCount = text.Split(ranges, '\x20', StringSplitOptions.RemoveEmptyEntries);

            if (sectionCount >= 1)
            {
                switch (text[ranges[0]])
                {
#if DEBUG
                    case "/_env":
#else
                    case "/env":
#endif
                    {
                        if (channel is not null)
                        {
                            _ = await channel.AddMessageAsync(
                                $"""
                                **BOT_tidus** is running on:
                                ```yml
                                Framework: {RuntimeInformation.FrameworkDescription}
                                OS       : {RuntimeInformation.OSDescription}
                                TimeZone : {TimeZoneInfo.Local}
                                ```
                                """,
                                false,
                                ct
                            );
                        }
                        break;
                    }
#if DEBUG
                    case "/_oisu":
#else
                    case "/oisu":
#endif
                    {
                        if (channel is not null)
                        {
                            _ = await channel.AddMessageAsync(GetRandomDecoratedOisu(), false, ct);
                        }
                        break;
                    }
                }
            }

            await base.OnMessageCreated(args, ct);
        }

        static string GetRandomDecoratedOisu()
        {
            ReadOnlySpan<string> decorations = ["", "large", "ex-large", "small", "rotate", "rotate-inv", "wiggle", "parrot", "zoom", "inversion", "turn", "turn-v", "happa", "pyon", "flashy", "pull", "atsumori", "stretch", "stretch-v", "marquee", "marquee-inv", "rainbow", "ascension", "shake", "party", "attract"];

            var r = Random.Shared;
            CollectionBuilder<char> builder = new(128);
            try
            {
                for (int i = 1; i <= 4; i++)
                {
                    builder.AppendLiteral($":oisu-{i}");
                    if (i == 4)
                    {
                        builder.AppendLiteral("yoko");
                    }
                    builder.AppendLiteral($".{decorations[r.Next(decorations.Length)]}:");
                }
                return builder.ToBasicString();
            }
            finally
            {
                builder.Dispose();
            }
        }
    }
}
