using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Extensions;
using DSharpPlus.Lavalink.EventArgs;
using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.Attributes;
using KekBot.Arguments;
using KekBot.Attributes;
using KekBot.Lib;
using KekBot.Menu;
using KekBot.Services;
using KekBot.Utils;
using RethinkDb.Driver.Model;
using static KekBot.Utils.Constants;

namespace KekBot.Commands {
    public class FunCommands : ApplicationCommandModule {
        private static readonly string[] EightBall =
        {
            "It is certain.",
            "It is decidedly so.",
            "Yes, definitely.",
            "You may rely on it.",
            "As I see it, yes.",
            "Most likely.",
            "Outlook good.",
            "Yes.",
            "Signs point to yes.",
            "Reply hazy, try again.",
            "Ask again later.",
            "Better not tell you now.",
            "Cannot predict now.",
            "Concentrate and ask again.",
            "Don't count on it.",
            "My reply is no.",
            "My sources say no.",
            "Outlook not so good.",
            "Very doubtful."
        };

        private readonly Randumb Rng = Randumb.Instance;
        
        [SlashCommand("8ball", "Ask the magic 8-ball a question!"), Category(Category.Fun)]
        async Task EightBallCommand(InteractionContext ctx,
            [Option("question", "The question to ask to the magic 8-ball.")] string question)
        {
            var emote = await CustomEmote.Get();
            await ctx.ReplyBasicAsync(
                $"{emote.Think} You asked: {question}\n\n🎱 8-Ball's response: {EightBall.RandomElement(Rng)}");
        }

        [SlashCommand("avatar", "Sends a larger version of the specified user's avatar.")]
        [Category(Category.Fun)]
        async Task AvatarCommand(InteractionContext ctx,
            [Option("user", "The user to pull the avatar from. (Returns yours if not specified.)")]
            DiscordUser? user = null,
            [Option(AvatarArgName, AvatarArgDescription)]
            AvatarPreference avaPref = default) =>
            await ctx.ReplyBasicAsync(
                await ((DiscordMember?)user ?? ctx.Member).AvatarUrlChecked(avaPref));
        
        [SlashCommand("flip", "Flips a coin."), Category(Category.Fun)]
        async Task FlipCommand(InteractionContext ctx)
        {
            var coin = Rng.OneOf("HEADS", "TAILS");
            await ctx.ReplyBasicAsync(
                $"{ctx.User.Mention} flipped the coin and it landed on... ***{coin}!***");
        }
        
        [SlashCommand("pick", "Has KekBot pick one of X choices for you."), Category(Category.Fun)]
        async Task PickCommand(
            InteractionContext ctx,
            [Option("choices", "Options separated with vertical bars, commas, or just spaces (for single-word choices).")]
            string choices
        ) {
            var choicesArray = ChoicesList.Parse(choices).Choices;
            await ctx.ReplyBasicAsync(choicesArray.Length switch {
                0 => "You haven't given me any choices, though...",
                1 => $"Well, I guess I'm choosing `{choicesArray.Single()}`, since you haven't given me anything else to pick...",
                _ => $"Hm... I think I'll go with `{choicesArray.RandomElement(Rng)}`.",
            });
        }
        
        [SlashCommandGroup("quote", "Add, List, Remove, or Get quotes from a list of quotes made in your server or channel.")]
        sealed class QuoteCommand : ApplicationCommandModule {
            private static readonly string NoQuotesError = "You have no quotes!";
            
            [SlashCommand("get", "Grabs a random quote from the list.")]
            async Task Get(InteractionContext ctx, [Option("quote_id", "A specific quote you want to get.")] long QuoteNumber = 0) {
                var set = await Settings.Get(ctx.Guild);
                if (set.Quotes.Count == 0) {
                    await ctx.ReplyBasicAsync(NoQuotesError, true);
                    return;
                }

                if (QuoteNumber == 0) {
                    await ctx.ReplyBasicAsync(set.Quotes.RandomElement());
                    return;
                }

                var toGet = (int)QuoteNumber - 1;
                if (toGet < 0) {
                    await ctx.ReplyBasicAsync("\"Yo you know what would be crazy? If I tried to get a *negative* quote! Hey wait a sec--\" ~You", true);
                    return;
                }
                if (set.Quotes.Count <= toGet) {
                    await ctx.ReplyBasicAsync($"You only have {set.Quotes.Count} quote{(set.Quotes.Count > 1 ? "s" : "")}! The id you typed exceeds that size.", true);
                    return;
                }

                var quote = set.Quotes[toGet];
                await ctx.ReplyBasicAsync(quote, false);
            }

            [SlashCommand("add", "Adds a quote to your list of quotes."), SlashRequireUserPermissions(Permissions.ManageMessages)]
            async Task Add(InteractionContext ctx, [Option("quote", "The quote you want to add.")] string Quote) {
                var set = await Settings.Get(ctx.Guild);
                set.Quotes.Add(Quote);
                await set.Save();
                await ctx.ReplyBasicAsync("Successfully added quote! 👍", true);
            }

            [SlashCommand("remove", "Removes a quote from your list of quotes."), SlashRequireUserPermissions(Permissions.ManageMessages)]
            async Task Remove(InteractionContext ctx, [Option("quote_id", "The number of the quote you wish to remove.")] long QuoteNumber) {
                var set = await Settings.Get(ctx.Guild);
                var toGet = (int)QuoteNumber - 1;
                if (set.Quotes.Count == 0) {
                    await ctx.ReplyBasicAsync(NoQuotesError, true);
                    return;
                }

                if (toGet < 0) {
                    await ctx.ReplyBasicAsync("\"Yo you know what would be crazy? If I tried to remove a *negative* quote! Hey wait a sec--\" ~You", true);
                    return;
                }

                if (set.Quotes.Count < toGet) {
                    await ctx.ReplyBasicAsync($"You only have {set.Quotes.Count} quote{(set.Quotes.Count > 1 ? "s" : "")}! The id you typed exceeds that size.", true);
                    return;
                }

                var quote = set.Quotes[toGet];
                set.Quotes.RemoveAt(toGet);
                await set.Save();
                await ctx.ReplyBasicAsync($"`{quote}`\n\nI've crumpled up this quote and thrown it away for you, Nya! :wastebasket:", true);
            }

            [SlashCommand("list", "Lists all of your quotes.")]
            async Task List(InteractionContext ctx) {
                var set = await Settings.Get(ctx.Guild);
                if (set.Quotes.Count == 0) {
                    await ctx.ReplyBasicAsync(NoQuotesError, true);
                    return;
                }

                var builder = new Paginator(ctx.Client.GetInteractivity()) {
                    Strings = set.Quotes.Select(q => q.Length > 200 ? q.Substring(0, 200) + "..." : q).ToList()
                };
                builder.SetGenericColor(ctx.Member.Color);
                builder.SetGenericText("Here are your quotes:");
                builder.Users.Add(ctx.Member.Id);
                await ctx.SendThinking();
                await builder.Display(ctx);
            }

            [SlashCommand("edit", "Edits a quote you specify."), RequireUserPermissions(DSharpPlus.Permissions.ManageMessages)]
            async Task Edit(InteractionContext ctx,
                [Option("quote_id", "The number of the quote you wish to edit.")] long QuoteNumber,
                [Option("quote", "The new contents of the quote.")] string Quote) {
                var set = await Settings.Get(ctx.Guild);
                var toGet = QuoteNumber - 1;
                if (set.Quotes.Count == 0) {
                    await ctx.ReplyBasicAsync("You have no quotes!");
                    return;
                }

                if (toGet < 0) {
                    await ctx.ReplyBasicAsync("\"Yo you know what would be crazy? If I tried to edit a *negative* quote! Hey wait a sec--\" ~You", true);
                    return;
                }
                if (set.Quotes.Count < toGet) {
                    await ctx.ReplyBasicAsync($"You only have {set.Quotes.Count} quote{(set.Quotes.Count > 1 ? "s" : "")}! The id you typed exceeds that size.", true);
                    return;
                }

                if (string.IsNullOrEmpty(Quote)) {
                    //TODO: requires questionnaire system to be ported over.
                    await ctx.ReplyBasicAsync("blech ew gross unfinished things");
                    return;
                }

                set.Quotes[(int)toGet] = Quote;
                await set.Save();
                await ctx.ReplyBasicAsync($"Successfully edited quote `{QuoteNumber}`.");
            }

            [SlashCommand("search", "Searches through quotes for specified text.")]
            async Task Search(InteractionContext ctx, [Option("query", "Text to search for.")] string Query) {
                var set = await Settings.Get(ctx.Guild);
                if (set.Quotes.Count == 0) {
                    await ctx.ReplyBasicAsync("You have no quotes!");
                    return;
                }
                var search = new List<string>();
                var reg = $"(?i).*({Query}).*";

                await ctx.SendThinking();
                for (int i = 0; i < set.Quotes.Count; i++) {
                    if (Regex.IsMatch(set.Quotes[i], reg)) search.Add($"`{i}.` {set.Quotes[i]}");
                }

                if (search.Count == 0) {
                    await ctx.EditBasicAsync($"No matches found for: {Query}");
                    return;
                }

                var builder = new Paginator(ctx.Client.GetInteractivity());

                builder.SetGenericText($"{search.Count} matches found for: {Query}.");
                builder.SetGenericColor(ctx.Member.Color);
                builder.Users.Add(ctx.Member.Id);
                builder.Strings = search;
                builder.NumberedItems = false;

                await builder.Display(ctx);
            }

            [SlashCommand("dump", "Dumps all quotes into a text file."), SlashRequireUserPermissions(Permissions.ManageMessages)]
            async Task Dump(InteractionContext ctx) {
                var set = await Settings.Get(ctx.Guild);
                if (set.Quotes.Count == 0) {
                    await ctx.ReplyBasicAsync("You have no quotes!", true);
                    return;
                }

                var dumper = new StringBuilder();
                for (int i = 0; i < set.Quotes.Count; i++) {
                    dumper.AppendLine($"{i + 1}. {set.Quotes[i]}");
                }
                var dump = Encoding.UTF8.GetBytes(dumper.ToString());
                var stream = new MemoryStream(dump);
                await ctx.ReplyAsync(new DiscordInteractionResponseBuilder().WithContent("Dump successful. You can view it below:").AddFile("quotes.txt", stream));
                await stream.DisposeAsync();
            }
        }
    }

    public class FunCommandsOld : BaseCommandModule {

        private static readonly string[] EightBall = { "It is certain.", "It is decidedly so.", "Yes, definitely.", "You may rely on it.",
        "As I see it, yes.", "Most likely.", "Outlook good.", "Yes.", "Signs point to yes.", "Reply hazy, try again.", "Ask again later.",
        "Better not tell you now.", "Cannot predict now.", "Concentrate and ask again.", "Don't count on it.", "My reply is no.", "My sources say no.",
        "Outlook not so good.", "Very doubtful." };

        private readonly Randumb Rng = Randumb.Instance;

        [Command("8ball"), Description("Ask the magic 8-ball a question!"), Category(Category.Fun)]
        async Task EightBallCommand(CommandContext ctx, [RemainingText, Description("The question to ask to the magic 8-ball.")] string question) {
            CustomEmote emote = await CustomEmote.Get();
            if (question == null) {
                await ctx.RespondAsync($"{emote.Think} I asked: Did {ctx.User.Username} give you a question?\n\n🎱 8-Ball's response: No, they didn't.");
            } else {
                await ctx.RespondAsync($"{emote.Think} You asked: {question}\n\n🎱 8-Ball's response: {EightBall.RandomElement(Rng)}");
            }
        }

        [Command("avatar"), Description("Sends a larger version of the specified user's avatar.")]
        [Aliases("ava"), Category(Category.Fun)]
        async Task AvatarCommand(CommandContext ctx, [Description("The user to pull the avatar from. (Returns your avatar if not specified)")] DiscordMember? user = null) {
            if (user == null) await ctx.RespondAsync(ctx.User.AvatarUrl);
            else await ctx.RespondAsync(user.AvatarUrl);
        }

        [Command("flip"), Description("Flips a coin."), Category(Category.Fun)]
        async Task FlipCommand(CommandContext ctx) {
            var coin = Rng.OneOf("HEADS", "TAILS");
            await ctx.RespondAsync($"{ctx.User.Username} flipped the coin and it landed on... ***{coin}!***");
        }

        [Command("pick"), Aliases("choose", "decide"), Category(Category.Fun)]
        [Description("Has KekBot pick one of X choices for you.")]
        async Task PickCommand(
            CommandContext ctx,
            [RemainingText, Description("Options separated with vertical bars, commas, or just spaces (for single-word choices).")]
            ChoicesList? choices = null
        ) {
            var choicesArray = choices?.Choices ?? Array.Empty<string>();
            await ctx.RespondAsync(choicesArray.Length switch {
                0 => "You haven't given me any choices, though...",
                1 => $"Well, I guess I'm choosing `{choicesArray.Single()}`, since you haven't given me anything else to pick...",
                _ => $"Hm... I think I'll go with `{choicesArray.RandomElement(Rng)}`.",
            });
        }

        [Group("profile"), Description("Lets you view and edit your profile, and also viewing other peoples profiles.")]
        sealed class ProfileCommand : BaseCommandModule {
            [GroupCommand, Priority(0)]
            async Task Profile(CommandContext ctx) {
                await ctx.TriggerTypingAsync();
                await ctx.RespondAsync("no");
            }

            [GroupCommand, Priority(1)]
            async Task Profile(CommandContext ctx, [Description("The user whos profile you want to see.")] DiscordMember User) {
                await ctx.TriggerTypingAsync();
                await ctx.RespondAsync($"{User.DisplayName} is kinda gay lol");
            }

            [Command("edit"), Description("Opens the menu to edit your profile.")]
            async Task Edit(CommandContext ctx) {
                await ctx.RespondAsync("there is no edit menu.");
            }
        }
        
    }
}
