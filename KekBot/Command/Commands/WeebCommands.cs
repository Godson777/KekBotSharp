using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Weeb.net;
using Weeb.net.Data;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using KekBot.Attributes;

namespace KekBot.Command.Commands {
    class WeebCommands : BaseCommandModule {

        // This gets initialized before the bot starts responding to commands
        private static WeebClient WeebClient = null!;

        private const string EmbedFooter = "Powered by Weeb.sh!";
        private const string FailureMsg = "Failed to retrieve image";

        internal static async Task InitializeAsync(string name, string version, string token) {
            WeebClient = new WeebClient(BotName: name, BotVersion: version);
            await WeebClient.Authenticate(token, TokenType.Wolke);
        }

        private async Task Base(CommandContext ctx, string type, string msg, IEnumerable<string>? tags = null) {
            await ctx.TriggerTypingAsync();

            var builder = new DiscordEmbedBuilder();
            RandomData? image = await WeebClient.GetRandomAsync(
                type: type,
                tags: tags ?? Array.Empty<string>(),
                //fileType: FileType.Any,
                //hidden: false,
                nsfw: ctx.Channel.IsNSFW ? NsfwSearch.True : NsfwSearch.False
            );
            if (image == null) {
                builder.WithTitle(FailureMsg);
            } else {
                builder.WithTitle(msg)
                    .WithImageUrl(new Uri(image.Url));
            }
            builder.WithFooter(EmbedFooter);

            await ctx.RespondAsync(embed: builder.Build());
        }

        [Command("awoo"), Description("AWOOOOOOOOO"), Category(Category.Weeb)]
        internal async Task Awoo(CommandContext ctx) {
            await Base(ctx, type: "awoo", msg: "AWOOOOO");
        }

    }
}
