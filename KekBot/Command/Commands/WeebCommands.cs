using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Weeb.net;
using Weeb.net.Data;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using KekBot.Attributes;
using KekBot.Utils;
using KekBot.Arguments;

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

        private static NsfwSearch CheckNsfw(CommandContext ctx, Flags flags) =>
            !ctx.Channel.IsNSFW
                ? NsfwSearch.False
                : flags.Has("nsfw") &&
                    Enum.TryParse(flags.GetOrEmpty("nsfw"), true, out NsfwSearch v) ? v : NsfwSearch.True;

        private static async Task Base(
            CommandContext ctx,
            string type,
            string msg,
            Flags flags = new Flags()
        ) {
            await ctx.TriggerTypingAsync();

            var tags = flags.GetNonEmpty("tags")?.Split(',') ?? Array.Empty<string>();
            var hidden = flags.IsNonEmpty("hidden");
            var nsfw = CheckNsfw(ctx, flags);
            await ctx.RespondAsync($"```\nDoing a search with:\n" +
                $"type: {type}\n" +
                $"tags: {tags} (length {tags.Length})\n" +
                $"hidden: {hidden}\n" +
                $"nsfw: {nsfw}\n" +
                $"```");

            var builder = new DiscordEmbedBuilder();
            RandomData? image = await WeebClient.GetRandomAsync(
                type: type,
                tags: tags,
                //fileType: FileType.Any,
                hidden: hidden,
                nsfw: nsfw
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

        private static async Task BaseMention(
            CommandContext ctx,
            DiscordMember? user,
            string type,
            string msg,
            Flags flags = new Flags()
        ) {
            if (user == null) {
                await ctx.RespondAsync("You didn't @mention any users!");
                return;
            }

            await ctx.TriggerTypingAsync();

            var tags = flags.GetNonEmpty("tags")?.Split(',') ?? Array.Empty<string>();
            var hidden = flags.IsNonEmpty("hidden");
            var nsfw = CheckNsfw(ctx, flags);
            await ctx.RespondAsync($"```\nDoing a search with:\n" +
                $"type: {type}\n" +
                $"tags: {tags} (length {tags.Length})\n" +
                $"hidden: {hidden}\n" +
                $"nsfw: {nsfw}\n" +
                $"```");

            var builder = new DiscordEmbedBuilder();
            RandomData? image = await WeebClient.GetRandomAsync(
                type: type,
                tags: tags,
                //fileType: FileType.Any,
                hidden: hidden,
                nsfw: nsfw
            );
            if (image == null) {
                builder.WithTitle(FailureMsg);
            } else {
                builder.WithTitle(string.Format(msg, user.DisplayName, ctx.Message.AuthorName()))
                    .WithImageUrl(new Uri(image.Url));
            }
            builder.WithFooter(EmbedFooter);

            await ctx.RespondAsync(embed: builder.Build());
        }

        [Command("get-types"), RequireOwner, Category(Category.Weeb)]
        internal async Task GetTypes(CommandContext ctx, [RemainingText] Flags flags = new Flags()) {
            var hidden = flags.IsNonEmpty("hidden");
            TypesData? types = await WeebClient.GetTypesAsync(hidden: hidden);
            await ctx.RespondAsync($"Types (includes hidden: {hidden}):\n" +
                (types == null
                    ? "couldn't fetch types"
                    : string.Join(", ", types.Types.OrderBy(s => s))));
        }

        [Command("get-tags"), RequireOwner, Category(Category.Weeb)]
        internal async Task GetTags(CommandContext ctx, [RemainingText] Flags flags = new Flags()) {
            var hidden = flags.IsNonEmpty("hidden");
            TagsData? tags = await WeebClient.GetTagsAsync(hidden: hidden);
            await ctx.RespondAsync($"Tags (includes hidden: {hidden}):\n" +
                (tags == null
                    ? "couldn't fetch tags"
                    : string.Join(", ", tags.Tags.OrderBy(s => s))));
        }

        [Command("awoo"), Description("AWOOOOOOOOO"), Category(Category.Weeb)]
        internal async Task Awoo(CommandContext ctx) =>
            await Base(ctx, type: "awoo", msg: "AWOOOOO");

        [Command("bite"), Description("Bites the living HECK out of someone."), Category(Category.Weeb)]
        internal async Task Bite(CommandContext ctx, [Description("@user")] DiscordMember? user = null) =>
            await BaseMention(ctx, user, type: "bite", msg: "{0} was bit by {1}!");

        [Command("cry"), Description(":((((((("), Category(Category.Weeb)]
        internal async Task Cry(CommandContext ctx) =>
            await Base(ctx, type: "cry", msg: ":CCCCCCC");

        [Command("cuddle"), Description("Cuddles a person."), Category(Category.Weeb)]
        internal async Task Cuddle(CommandContext ctx, [Description("@user")] DiscordMember? user = null) =>
            await BaseMention(ctx, user, type: "cuddle", msg: "{1} was cuddled by {2}.");

    }
}
