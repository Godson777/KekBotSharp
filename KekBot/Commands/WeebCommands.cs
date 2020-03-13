using System;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Weeb.net;
using Weeb.net.Data;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using KekBot.Arguments;
using KekBot.Attributes;
using KekBot.Lib;
using KekBot.Utils;

namespace KekBot.Commands {
    class WeebCommands : IHasFakeCommands {
        internal struct WeebCmdInfo : ICommandInfo {
            public string Name { get; }
            public ImmutableArray<string> Aliases => ImmutableArray.Create(Array.Empty<string>());
            public string Description { get; }
            public Category Category => Category.Weeb;
            public ImmutableArray<ICommandOverloadInfo> Overloads { get; }
            public DSharpPlus.CommandsNext.Command? Cmd => null;

            public string Msg { get; }
            public bool MentionsUser { get; }

            private static readonly Regex FormatThingies = new Regex("{(?<num>\\d*)}");

            [SuppressMessage("Globalization", "CA1303:Do not pass literals as localized parameters", Justification = "Only thrown at bot startup")]
            public WeebCmdInfo(string type, string description, string msg, bool mentionsUser = false) {
                Name = type;
                Description = description;
                if (mentionsUser) {
                    Overloads = Util.ImmutableArrayFromSingle<ICommandOverloadInfo>(
                        new WeebOverloadMention()
                    );
                    foreach (var match in FormatThingies.Matches(msg).AsEnumerable()) {
                        if (!int.TryParse(match.Groups["num"].Value, out var num) ||
                            (num != 0 && num != 1)) {
                            throw new ArgumentOutOfRangeException(paramName: nameof(msg),
                                message: "Msg cannot reference substitutions other than 0 and 1");
                        }
                    }
                } else {
                    Overloads = Util.ImmutableArrayFromSingle<ICommandOverloadInfo>(
                        new WeebOverload()
                    );
                }
                Msg = msg;
                MentionsUser = mentionsUser;
            }

            public bool Equals([AllowNull] ICommandInfo other) => Name == other.Name;
        }

        internal struct WeebOverload : ICommandOverloadInfo {
            public ImmutableArray<ICommandArgumentInfo> Arguments => ImmutableArray<ICommandArgumentInfo>.Empty;
            public int Priority => 0;
        }

        internal struct WeebOverloadMention : ICommandOverloadInfo {
            public ImmutableArray<ICommandArgumentInfo> Arguments => ImmutableArray.Create(new[] {
                (ICommandArgumentInfo)new WeebArgMention()
            });
            public int Priority => 0;
        }

        internal struct WeebArgMention : ICommandArgumentInfo {
            public string Name => "user";
            public string Description => "@user";
            public bool IsOptional => false;
            public bool IsHidden => false;
        }

        internal static readonly WeebCmdInfo[] FakeCommandInfo = new WeebCmdInfo[] {
            new WeebCmdInfo("awoo", "AWOOOOOOOOO",
                msg: "AWOOOOO"),
            new WeebCmdInfo("bite", "Bites the living HECK out of someone.",
                msg: "{0} was bit by {1}!", mentionsUser: true),
            new WeebCmdInfo("cry", ":(((((((",
                msg: ":CCCCCCC"),
            new WeebCmdInfo("cuddle", "Cuddles a person.",
                msg: "{0} was cuddled by {1}.", mentionsUser: true),
        };

        ICommandInfo[] IHasFakeCommands.FakeCommandInfo => FakeCommandInfo.Cast<ICommandInfo>().ToArray();

        private static readonly string[] FakeCommands = FakeCommandInfo.Select(cmdInfo => cmdInfo.Name).ToArray();
        string[] IHasFakeCommands.FakeCommands => FakeCommands;

        // This gets initialized before the bot starts responding to commands
        private static WeebClient WeebClient = null!;

        private const string EmbedFooter = "Powered by Weeb.sh!";
        private const string FailureMsg = "Failed to retrieve image";

        internal static async Task InitializeAsync(string name, string version, string token) {
            WeebClient = new WeebClient(BotName: name, BotVersion: version);
            await WeebClient.Authenticate(token, TokenType.Wolke);
        }

        async Task IHasFakeCommands.HandleFakeCommand(CommandContext ctx, string cmdName) {
            Util.Assert(FakeCommands.Contains(cmdName), elsePanicWith: "how did this happen");
            Util.Assert(FakeCommandInfo.Any(cmd => cmd.Name == cmdName), elsePanicWith: "how did this happen");

            var cmdInfo = FakeCommandInfo.First(cmdInfo => cmdInfo.Name == cmdName);
            if (cmdInfo.MentionsUser) {
                var rawArgs = ctx.RawArguments;
                var user = rawArgs.Count >= 1
                    ? await rawArgs[0].ConvertArgAsync<DiscordMember>(ctx)
                    : null;
                await BaseMention(ctx, user, type: cmdName, msg: cmdInfo.Msg);
            } else {
                await Base(ctx, type: cmdName, msg: cmdInfo.Msg);
            }
        }

        private static NsfwSearch CheckNsfw(CommandContext ctx, FlagArgs flags) =>
            !ctx.Channel.IsNSFW
                ? NsfwSearch.False
                : flags.ParseEnum<NsfwSearch>("nsfw") ?? NsfwSearch.True;

        private static async Task Base(
            CommandContext ctx,
            string type,
            string msg,
            FlagArgs flags = new FlagArgs()
        ) {
            await ctx.TriggerTypingAsync();

            var tags = flags.Get("tags").NonEmpty()?.Split(',') ?? Array.Empty<string>();
            var hidden = flags.ParseBool("hidden") ?? false;
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
            FlagArgs flags = new FlagArgs()
        ) {
            if (user == null) {
                await ctx.RespondAsync("You didn't @mention any users!");
            } else {
                await Base(ctx, type: type, msg: string.Format(msg, user.DisplayName, ctx.Message.AuthorName()), flags);
            }
        }

        [Command("get-types"), RequireOwner, Category(Category.Weeb)]
        internal async Task GetTypes(CommandContext ctx, [RemainingText] FlagArgs flags = new FlagArgs()) {
            var hidden = flags.ParseBool("hidden") ?? false;
            TypesData? types = await WeebClient.GetTypesAsync(hidden: hidden);
            await ctx.RespondAsync($"Types (includes hidden: {hidden}):\n" +
                (types == null
                    ? "couldn't fetch types"
                    : string.Join(", ", types.Types.OrderBy(s => s))));
        }

        [Command("get-tags"), RequireOwner, Category(Category.Weeb)]
        internal async Task GetTags(CommandContext ctx, [RemainingText] FlagArgs flags = new FlagArgs()) {
            var hidden = flags.ParseBool("hidden") ?? false;
            TagsData? tags = await WeebClient.GetTagsAsync(hidden: hidden);
            await ctx.RespondAsync($"Tags (includes hidden: {hidden}):\n" +
                (tags == null
                    ? "couldn't fetch tags"
                    : string.Join(", ", tags.Tags.OrderBy(s => s))));
        }

        //[Command("awoo"), Description("AWOOOOOOOOO"), Category(Category.Weeb)]
        //internal async Task Awoo(CommandContext ctx) =>
        //    await Base(ctx, type: "awoo", msg: "AWOOOOO");

        //[Command("bite"), Description("Bites the living HECK out of someone."), Category(Category.Weeb)]
        //internal async Task Bite(CommandContext ctx, [Description("@user")] DiscordMember? user = null) =>
        //    await BaseMention(ctx, user, type: "bite", msg: "{0} was bit by {1}!");

        //[Command("cry"), Description(":((((((("), Category(Category.Weeb)]
        //internal async Task Cry(CommandContext ctx) =>
        //    await Base(ctx, type: "cry", msg: ":CCCCCCC");

        //[Command("cuddle"), Description("Cuddles a person."), Category(Category.Weeb)]
        //internal async Task Cuddle(CommandContext ctx, [Description("@user")] DiscordMember? user = null) =>
        //    await BaseMention(ctx, user, type: "cuddle", msg: "{1} was cuddled by {2}.");

    }
}
