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
using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.Attributes;
using KekBot.Arguments;
using KekBot.Attributes;
using KekBot.Lib;
using KekBot.Utils;

namespace KekBot.Commands {
    [SlashCommandGroup("weeb", "Powered by Weeb.sh!")]
    class WeebCommands : ApplicationCommandModule {

        private const string FlagArgName = "flags";
        private const string FlagArgDescription =
            "Advanced options. Ask us if you really want to know how to use them.";

        [SlashCommandGroup("interact", "Interact with someone else")]
        class MentionWeebs : ApplicationCommandModule
        {
            private const string UserArgDescription = "User to interact with";
            
            public WeebCommandsBase Base { private get; set; }
        
            public override async Task<bool> BeforeSlashExecutionAsync(InteractionContext ctx)
            {
                await Base.Initialized;
                return true;
            }
        
            [SlashCommand("bite", "Bites the living HECK out of someone.")]
            public async Task bite(InteractionContext ctx,
                [Option("user", UserArgDescription)] DiscordUser user,
                [Option(FlagArgName, FlagArgDescription)] string flagsStr = "") =>
                await Base.FetchAndPostWithMention(ctx, "bite", "{0} was bit by {1}!", (DiscordMember)user, flagsStr);
                
            [SlashCommand("cuddle", "Cuddles a person.")]
            public async Task cuddle(InteractionContext ctx,
                [Option("user", UserArgDescription)] DiscordUser user,
                [Option(FlagArgName, FlagArgDescription)] string flagsStr = "") =>
                await Base.FetchAndPostWithMention(ctx, "cuddle", "{0} was cuddled by {1}.", (DiscordMember)user, flagsStr);
                
            [SlashCommand("hug", "Hugs a person.")]
            public async Task hug(InteractionContext ctx,
                [Option("user", UserArgDescription)] DiscordUser user,
                [Option(FlagArgName, FlagArgDescription)] string flagsStr = "") =>
                await Base.FetchAndPostWithMention(ctx, "hug", "{0} was hugged by {1}.", (DiscordMember)user, flagsStr);
                
            [SlashCommand("kiss", "Kisses a person.")]
            public async Task kiss(InteractionContext ctx,
                [Option("user", UserArgDescription)] DiscordUser user,
                [Option(FlagArgName, FlagArgDescription)] string flagsStr = "") =>
                await Base.FetchAndPostWithMention(ctx, "kiss", "{0} was kissed by {1}.", (DiscordMember)user, flagsStr);
                
            [SlashCommand("lick", "Licks a person.")]
            public async Task lick(InteractionContext ctx,
                [Option("user", UserArgDescription)] DiscordUser user,
                [Option(FlagArgName, FlagArgDescription)] string flagsStr = "") =>
                await Base.FetchAndPostWithMention(ctx, "lick", "{0} was licked by {1}!", (DiscordMember)user, flagsStr);
                
            [SlashCommand("nom", "nomnomnomnomnomnomnomnom")]
            public async Task nom(InteractionContext ctx,
                [Option("user", UserArgDescription)] DiscordUser user,
                [Option(FlagArgName, FlagArgDescription)] string flagsStr = "") =>
                await Base.FetchAndPostWithMention(ctx, "nom", "{0} got nomed on by {1}! omnomnom...", (DiscordMember)user, flagsStr);
                
            [SlashCommand("pat", "Pats a person.")]
            public async Task pat(InteractionContext ctx,
                [Option("user", UserArgDescription)] DiscordUser user,
                [Option(FlagArgName, FlagArgDescription)] string flagsStr = "") =>
                await Base.FetchAndPostWithMention(ctx, "pat", "{0} got pat by {1}.", (DiscordMember)user, flagsStr);
                
            [SlashCommand("poke", "Lets you poke a user and annoy them. >:3")]
            public async Task poke(InteractionContext ctx,
                [Option("user", UserArgDescription)] DiscordUser user,
                [Option(FlagArgName, FlagArgDescription)] string flagsStr = "") =>
                await Base.FetchAndPostWithMention(ctx, "poke", "Poke!", (DiscordMember)user, flagsStr);
                
            [SlashCommand("punch", "Punch someone in the face!")]
            public async Task punch(InteractionContext ctx,
                [Option("user", UserArgDescription)] DiscordUser user,
                [Option(FlagArgName, FlagArgDescription)] string flagsStr = "") =>
                await Base.FetchAndPostWithMention(ctx, "punch", "{0} got punched by {1}!", (DiscordMember)user, flagsStr);
                
            [SlashCommand("slap", "Slaps a person.")]
            public async Task slap(InteractionContext ctx,
                [Option("user", UserArgDescription)] DiscordUser user,
                [Option(FlagArgName, FlagArgDescription)] string flagsStr = "") =>
                await Base.FetchAndPostWithMention(ctx, "slap", "{0} was slapped by {1}!", (DiscordMember)user, flagsStr);
                
            [SlashCommand("tickle", "Tickles a person.")]
            public async Task tickle(InteractionContext ctx,
                [Option("user", UserArgDescription)] DiscordUser user,
                [Option(FlagArgName, FlagArgDescription)] string flagsStr = "") =>
                await Base.FetchAndPostWithMention(ctx, "tickle", "{0} was ticked to death by {1}!", (DiscordMember)user, flagsStr);
        }

        [SlashCommandGroup("solo", "Things performed alone")]
        class SoloWeebs : ApplicationCommandModule
        {
            public WeebCommandsBase Base { private get; set; }

            public override async Task<bool> BeforeSlashExecutionAsync(InteractionContext ctx)
            {
                await Base.Initialized;
                return true;
            }
            
            [SlashCommand("get-types", "debugging"), SlashRequireOwner, Category(Category.Weeb)]
            internal async Task GetTypes(InteractionContext ctx, [Option(FlagArgName, FlagArgDescription)] string flagsStr = "") {
                var flags = FlagArgs.ParseString(flagsStr) ?? new FlagArgs();
                var hidden = flags.ParseBool("hidden") ?? false;
                TypesData? types = await Base.Client.GetTypesAsync(hidden: hidden);
                await ctx.ReplyBasicAsync($"Types (includes hidden: {hidden}):\n" +
                                          (types == null
                                              ? "couldn't fetch types"
                                              : string.Join(", ", types.Types.OrderBy(s => s))));
            }
            
            [SlashCommand("get-tags", "debugging"), SlashRequireOwner, Category(Category.Weeb)]
            internal async Task GetTags(InteractionContext ctx, [Option(FlagArgName, FlagArgDescription)] string flagsStr = "") {
                var flags = FlagArgs.ParseString(flagsStr) ?? new FlagArgs();
                var hidden = flags.ParseBool("hidden") ?? false;
                TagsData? tags = await Base.Client.GetTagsAsync(hidden: hidden);
                await ctx.ReplyBasicAsync($"Tags (includes hidden: {hidden}):\n" +
                                          (tags == null
                                              ? "couldn't fetch tags"
                                              : string.Join(", ", tags.Tags.OrderBy(s => s))));
            }

            [SlashCommand("awoo", "AWOOOOOOOOO")]
            public async Task awoo(InteractionContext ctx,
                [Option(FlagArgName, FlagArgDescription)] string flagsStr = "") =>
                await Base.FetchAndPost(ctx, "awoo", "AWOOOOO", flagsStr);
                
            [SlashCommand("cry", ":(((((((")]
            public async Task cry(InteractionContext ctx,
                [Option(FlagArgName, FlagArgDescription)] string flagsStr = "") =>
                await Base.FetchAndPost(ctx, "cry", ":CCCCCCC", flagsStr);
                
            [SlashCommand("dab", "<o/")]
            public async Task dab(InteractionContext ctx,
                [Option(FlagArgName, FlagArgDescription)] string flagsStr = "") =>
                await Base.FetchAndPost(ctx, "dab", @"<o/ \o>", flagsStr);
                
            [SlashCommand("dance", "ᕕ( ᐛ )ᕗ")]
            public async Task dance(InteractionContext ctx,
                [Option(FlagArgName, FlagArgDescription)] string flagsStr = "") =>
                await Base.FetchAndPost(ctx, "dance", "ᕕ( ᐛ )ᕗ", flagsStr);
                
            [SlashCommand("deredere", "Because we couldn't get a tsundere command")]
            public async Task deredere(InteractionContext ctx,
                [Option(FlagArgName, FlagArgDescription)] string flagsStr = "") =>
                await Base.FetchAndPost(ctx, "deredere", "❤❤❤❤", flagsStr);
                
            [SlashCommand("lewd", "For those l-lewd moments...")]
            public async Task lewd(InteractionContext ctx,
                [Option(FlagArgName, FlagArgDescription)] string flagsStr = "") =>
                await Base.FetchAndPost(ctx, "lewd", "Did someone say l-lewd?", flagsStr);
                
            [SlashCommand("neko", "Because we all need nekos in our lives.")]
            public async Task neko(InteractionContext ctx,
                [Option(FlagArgName, FlagArgDescription)] string flagsStr = "") =>
                await Base.FetchAndPost(ctx, "neko", "Did someone call for a neko? :3", flagsStr);
                
            [SlashCommand("owo", "OwO WHAT THE FUCK IS THIS")]
            public async Task owo(InteractionContext ctx,
                [Option(FlagArgName, FlagArgDescription)] string flagsStr = "") =>
                await Base.FetchAndPost(ctx, "owo", "OwO", flagsStr);
                
            [SlashCommand("pout", ":(")]
            public async Task pout(InteractionContext ctx,
                [Option(FlagArgName, FlagArgDescription)] string flagsStr = "") =>
                await Base.FetchAndPost(ctx, "pout", ":C", flagsStr);
                
            [SlashCommand("shrug", @"¯\_(ツ)_/¯")]
            public async Task shrug(InteractionContext ctx,
                [Option(FlagArgName, FlagArgDescription)] string flagsStr = "") =>
                await Base.FetchAndPost(ctx, "shrug", "Huh?", flagsStr);
                
            [SlashCommand("sleepy", "I'm not sleepy, YOU'RE sleepy!")]
            public async Task sleepy(InteractionContext ctx,
                [Option(FlagArgName, FlagArgDescription)] string flagsStr = "") =>
                await Base.FetchAndPost(ctx, "sleepy", "Yawn...", flagsStr);
                
            [SlashCommand("smug", "Because all you weebs ever do is smug >:C")]
            public async Task smug(InteractionContext ctx,
                [Option(FlagArgName, FlagArgDescription)] string flagsStr = "") =>
                await Base.FetchAndPost(ctx, "smug", "Heh.", flagsStr);
                
            [SlashCommand("stare", "👀")]
            public async Task stare(InteractionContext ctx,
                [Option(FlagArgName, FlagArgDescription)] string flagsStr = "") =>
                await Base.FetchAndPost(ctx, "stare", "👀", flagsStr);
                
            [SlashCommand("thumbsup", "Because you definitely need virtual thumbs")]
            public async Task thumbsup(InteractionContext ctx,
                [Option(FlagArgName, FlagArgDescription)] string flagsStr = "") =>
                await Base.FetchAndPost(ctx, "thumbsup", "This has my approval.", flagsStr);
                
            [SlashCommand("wag", "AWOOO 2: Electric Boogaloo")]
            public async Task wag(InteractionContext ctx,
                [Option(FlagArgName, FlagArgDescription)] string flagsStr = "") =>
                await Base.FetchAndPost(ctx, "wag", ":3", flagsStr);
        }
        
        /*
         * @todo Move `discord` command from meme category to weeb category?
         * @body Either way, it's just a weeb.sh command but with a different category. Dunno what to do about this one.
         */

    }
}
