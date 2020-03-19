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
    class WeebCommands : BaseCommandModule, IHasFakeCommands, INeedsInitialized {

        internal static readonly WeebCmdInfo[] FakeCommandInfo = new WeebCmdInfo[] {
            new WeebCmdInfo("awoo", "AWOOOOOOOOO",
                "AWOOOOO"),
            new WeebCmdInfo("bite", "Bites the living HECK out of someone.",
                "{0} was bit by {1}!", mentionsUser: true),
            new WeebCmdInfo("cry", ":(((((((",
                ":CCCCCCC"),
            new WeebCmdInfo("cuddle", "Cuddles a person.",
                "{0} was cuddled by {1}.", mentionsUser: true),
            new WeebCmdInfo("dab", "<o/",
                @"<o/ \o>"),
            new WeebCmdInfo("dance", "ᕕ( ᐛ )ᕗ",
                "ᕕ( ᐛ )ᕗ"),
            new WeebCmdInfo("deredere", "Because we couldn't get a tsundere command",
                "❤❤❤❤"),
            new WeebCmdInfo("hug", "Hugs a person.",
                "{0} was hugged by {1}.", mentionsUser: true),
            new WeebCmdInfo("kiss", "Kisses a person.",
                "{0} was kissed by {1}.", mentionsUser: true),
            new WeebCmdInfo("lewd", "For those l-lewd moments...",
                "Did someone say l-lewd?"),
            new WeebCmdInfo("lick", "Licks a person.",
                "{0} was licked by {1}!", mentionsUser: true),
            new WeebCmdInfo("neko", "Because we all need nekos in our lives.",
                "Did someone call for a neko? :3"),
            new WeebCmdInfo("nom", "nomnomnomnomnomnomnomnom",
                "{0} got nomed on by {1}! omnomnom...", mentionsUser: true),
            new WeebCmdInfo("owo", "OwO WHAT THE FUCK IS THIS",
                "OwO"),
            new WeebCmdInfo("pat", "Pats a person.",
                "{0} got pat by {1}.", mentionsUser: true),
            new WeebCmdInfo("poke", "Lets you poke a user and annoy them. >:3",
                "Poke!", mentionsUser: true),
            new WeebCmdInfo("pout", ":(",
                ":C"),
            new WeebCmdInfo("punch", "Punch someone in the face!",
                "{0} got punched by {1}!", mentionsUser: true),
            new WeebCmdInfo("shrug", @"¯\_(ツ)_/¯",
                "Huh?"),
            new WeebCmdInfo("slap", "Slaps a person.",
                "{0} was slapped by {1}!", mentionsUser: true),
            new WeebCmdInfo("sleepy", "I'm not sleepy, YOU'RE sleepy!",
                "Yawn..."),
            new WeebCmdInfo("smug", "Because all you weebs ever do is smug >:C",
                "Heh."),
            new WeebCmdInfo("stare", "👀",
                "👀"),
            new WeebCmdInfo("thumbsup", "Because you definitely need virtual thumbs",
                "This has my approval."),
            new WeebCmdInfo("tickle", "Tickles a person.",
                "{0} was ticked to death by {1}!", mentionsUser: true),
            new WeebCmdInfo("wag", "AWOOO 2: Electric Boogaloo",
                ":3"),
        };

        ICommandInfo[] IHasFakeCommands.FakeCommandInfo => Array.ConvertAll(FakeCommandInfo, cmdInfo => (ICommandInfo)cmdInfo);

        private static readonly string[] FakeCommands = Array.ConvertAll(FakeCommandInfo, cmdInfo => cmdInfo.Name);
        string[] IHasFakeCommands.FakeCommands => FakeCommands;

        // This gets initialized before the bot starts responding to commands
        private readonly WeebClient WeebClient;

        private const string EmbedFooter = "Powered by Weeb.sh!";
        private const string FailureMsg = "Failed to retrieve image";
        private const string FailureMsgNsfw = "This type probably has no NSFW images.";
        private const string FailureMsgSearch = "There were probably no results for your search.";

        /// <summary>
        /// Await this task to wait for this to be ready to handle weeb.sh requests.
        /// </summary>
        internal Task Initialized;

        Task INeedsInitialized.Initialize() => Initialized;

        internal class WeebCmdsCtorArgs {
            public string BotName { get; }
            public string BotVersion { get; }
            public string WeebToken { get; }
            public WeebCmdsCtorArgs(string botName, string botVersion, string weebToken) {
                BotName = botName;
                BotVersion = botVersion;
                WeebToken = weebToken;
            }
        }

        public WeebCommands(WeebCmdsCtorArgs args) {
            WeebClient = new WeebClient(BotName: args.BotName, BotVersion: args.BotVersion);
            Initialized = WeebClient.Authenticate(args.WeebToken, TokenType.Wolke);
        }

        async Task IHasFakeCommands.HandleFakeCommand(CommandContext ctx, string cmdName) {
            Util.Assert(FakeCommands.Contains(cmdName), elsePanicWith: "how did this happen");
            Util.Assert(FakeCommandInfo.Any(cmdInfo => cmdInfo.Name == cmdName), elsePanicWith: "how did this happen");

            var cmdInfo = FakeCommandInfo.First(cmdInfo => cmdInfo.Name == cmdName);
            var argStr = ctx.GetRawArgString(cmdName);
            if (cmdInfo.MentionsUser) {
                var flags = FlagArgs.ParseString(argStr, out var nonFlagsStr) ?? new FlagArgs();
                var user = nonFlagsStr.Length == 0
                    ? null
                    : await Util.ConvertArgAsync<DiscordMember>(nonFlagsStr, ctx);
                await BaseMention(ctx, user, type: cmdName, msg: cmdInfo.Msg, flags);
            } else {
                var flags = FlagArgs.ParseString(argStr) ?? new FlagArgs();
                await Base(ctx, type: cmdName, msg: cmdInfo.Msg, flags);
            }
        }

        private static NsfwSearch CheckNsfw(CommandContext ctx, FlagArgs flags) =>
            !ctx.Channel.IsNSFW
                ? NsfwSearch.False
                : flags.ParseEnum<NsfwSearch>("nsfw") ?? NsfwSearch.True;

        private async Task Base(
            CommandContext ctx,
            string type,
            string msg,
            FlagArgs flags
        ) {
            await ctx.TriggerTypingAsync();

            var tags = flags.Get("tags").NonEmpty()?.Split(',') ?? Array.Empty<string>();
            var hiddenFlag = flags.ParseBool("hidden");
            var requestHidden = hiddenFlag ?? false;
            var nsfwFlag = flags.ParseEnum<NsfwSearch>("nsfw");
            var requestNsfw = ctx.Channel.IsNSFW
                ? (nsfwFlag ?? NsfwSearch.True)
                : NsfwSearch.False;
            await ctx.RespondAsync($"```\nDoing a search with:\n" +
                $"type: {type}\n" +
                $"tags: {tags} (length {tags.Length})\n" +
                $"hidden: {requestHidden}\n" +
                $"nsfw: {requestNsfw}\n" +
                $"```");

            var builder = new DiscordEmbedBuilder();
            RandomData? image = await WeebClient.GetRandomAsync(
                type: type,
                tags: tags,
                //fileType: FileType.Any,
                hidden: requestHidden,
                nsfw: requestNsfw
            );
            if (image == null) {
                builder.WithTitle(FailureMsg);

                if (tags.Length > 0 || hiddenFlag != null) {
                    builder.WithDescription(FailureMsgSearch);
                } else if (requestNsfw == NsfwSearch.Only) {
                    builder.WithDescription(FailureMsgNsfw);
                }
            } else {
                builder.WithTitle(msg).WithImageUrl(new Uri(image.Url));

                var tagStr = Util.Join(image.Tags, tag => {
                    var extraInfo = string.Join("; ", new string[] {
                        tag.Hidden ? "hidden" : "",
                        string.IsNullOrEmpty(tag.User) ? "" : $"user {tag.User}",
                    }.Where(s => s.Length > 0));
                    return tag.Name + (extraInfo.Length > 0
                        ? $" ({extraInfo})"
                        : "");
                });
                builder.AddField("Tags", tagStr, inline: true);

                if (nsfwFlag != null || ctx.Channel.IsNSFW || image.Nsfw) {
                    if (!ctx.Channel.IsNSFW && image.Nsfw) {
                        await ctx.RespondAsync("For some reason Weeb.sh gave me a NSFW image, but this is a SFW channel!");
                        return;
                    }

                    var nsfwStr = image.Nsfw
                        ? "yes"
                        : (ctx.Channel.IsNSFW ? "no" : "not allowed in SFW channels");
                    builder.AddField("NSFW", nsfwStr, inline: true);
                }

                if (hiddenFlag != null || image.Hidden) {
                    builder.AddField("Hidden", image.Hidden ? "yes" : "no", inline: true);
                }
            }
            builder.WithFooter(EmbedFooter);

            await ctx.RespondAsync(embed: builder.Build());
        }

        private async Task BaseMention(
            CommandContext ctx,
            DiscordMember? user,
            string type,
            string msg,
            FlagArgs flags
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

        internal struct WeebCmdInfo : ICommandInfo {
            public string Name { get; }
            public ImmutableArray<string> Aliases => ImmutableArray.Create(Array.Empty<string>());
            public string Description { get; }
            public Category Category => Category.Weeb;
            public ImmutableArray<ICommandOverloadInfo> Overloads { get; }
            public Command? Cmd => null;

            public string Msg { get; }
            public bool MentionsUser { get; }

            private static readonly Regex FormatThingies = new Regex("{(?<num>\\d*)}");

            [SuppressMessage("Globalization", "CA1303:Do not pass literals as localized parameters", Justification = "Only thrown at bot startup")]
            public WeebCmdInfo(string type, string description, string msg, bool mentionsUser = false) {
                Name = type;
                Description = description;

                if (msg.Contains("%s", StringComparison.Ordinal)) {
                    throw new ArgumentException(paramName: nameof(msg),
                        message: "You forgot to replace the '%s' things, dummy.");
                }
                var matches = FormatThingies.Matches(msg).AsEnumerable();
                if (mentionsUser) {
                    Overloads = Util.ImmutableArrayFromSingle<ICommandOverloadInfo>(
                        new WeebOverloadMention()
                    );
                    foreach (var match in matches) {
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
                    if (matches.Any()) {
                        throw new ArgumentException(paramName: nameof(msg),
                            message: "Msg cannot use substitutions when there are no mentions to substitute");
                    }
                }

                Msg = msg;
                MentionsUser = mentionsUser;
            }

            public bool Equals([AllowNull] ICommandInfo other) => Name == other.Name;
        }

        internal struct WeebOverload : ICommandOverloadInfo {
            public ImmutableArray<ICommandArgumentInfo> Arguments => ImmutableArray.Create(new[] {
                (ICommandArgumentInfo)new WeebArgFlags()
            });
            public int Priority => 0;
        }

        internal struct WeebOverloadMention : ICommandOverloadInfo {
            public ImmutableArray<ICommandArgumentInfo> Arguments => ImmutableArray.Create(new[] {
                // Flags are parsed before this arg
                (ICommandArgumentInfo)new WeebArgMention()
            });
            public int Priority => 0;
        }

        internal struct WeebArgFlags : ICommandArgumentInfo {
            public string Name => "flags";
            public string Description => "";
            public bool IsOptional => true;
            public bool IsHidden => true;
        }

        internal struct WeebArgMention : ICommandArgumentInfo {
            public string Name => "user";
            public string Description => "@user";
            public bool IsOptional => false;
            public bool IsHidden => false;
        }

    }
}
