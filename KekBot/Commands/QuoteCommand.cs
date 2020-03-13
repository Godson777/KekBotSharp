using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Interactivity;
using KekBot.Attributes;
using KekBot.Menu;
using KekBot.Utils;
using RethinkDb.Driver.Ast;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace KekBot.Command.Commands {
    [Group("quote"), Description("Grabs a random quote from a list of quotes made in your server."), Category(Category.Fun), Aliases("q")]
    public class QuoteCommand : BaseCommandModule {
        [GroupCommand, Priority(0)]
        public async Task Quote(CommandContext ctx) {
            var set = await Settings.Get(ctx.Guild);
            if (set.Quotes.Count == 0) {
                await ctx.RespondAsync("You have no quotes!");
                return;
            }
            await ctx.RespondAsync(set.Quotes.RandomElement());
        }

        [GroupCommand, Priority(1)]
        public async Task Quote(CommandContext ctx, [Description("The specific quote you want pulled out.")] int QuoteNumber) {
            var set = await Settings.Get(ctx.Guild);
            if (set.Quotes.Count == 0) {
                await ctx.RespondAsync("You have no quotes!");
                return;
            }
            var toGet = QuoteNumber - 1;
            if (toGet < 0 || toGet > set.Quotes.Count) {
                await ctx.RespondAsync("\"Here, let me just get a quote that doesn't exist... Oh, wait...\" ~You");
                return;
            }
            await ctx.RespondAsync(set.Quotes[toGet]);
        }

        [Command("add"), Description("Adds a quote to your list of quotes."), RequireUserPermissions(DSharpPlus.Permissions.ManageMessages)]
        public async Task Add(CommandContext ctx, [Description("The quote you want to add."), RemainingText, Required] string Quote = "") {
            var set = await Settings.Get(ctx.Guild);
            if (string.IsNullOrEmpty(Quote)) {
                //do nothing for now, requires questionnaire system to be ported over.
                await ctx.RespondAsync("blech ew gross unfinished things");
                return;
            }
            await AddQuote(ctx, set, Quote);
        }

        private async Task AddQuote(CommandContext ctx, Settings set, string quote) {
            set.Quotes.Add(quote);
            await set.Save();
            await ctx.RespondAsync("Successfully added quote! 👍");
        }

        [Command("remove"), Description("Removes a quote from your list of quotes."), RequireUserPermissions(DSharpPlus.Permissions.ManageMessages)]
        public async Task Remove(CommandContext ctx, [Description("The number of the quote you wish to remove."), Required] int QuoteNumber = -1) {
            var set = await Settings.Get(ctx.Guild);
            var toGet = QuoteNumber - 1;
            if (set.Quotes.Count == 0) {
                await ctx.RespondAsync("You have no quotes!");
                return;
            }

            if (QuoteNumber == -1) {
                await ctx.RespondAsync("No quote specified.");
                return;
            }
            if (set.Quotes.Count < toGet) {
                //do nothing rn im lazy
                return;
            }

            await RemoveQuote(ctx, set, toGet);
        }

        private async Task RemoveQuote(CommandContext ctx, Settings set, int quoteNum) {
            var quote = set.Quotes[quoteNum];
            set.Quotes.RemoveAt(quoteNum);
            await set.Save();
            await ctx.RespondAsync($"Successfully removed quote: `{quote}`.");
        }

        [Command("list"), Description("Lists all of your quotes.")]
        public async Task List(CommandContext ctx) {
            var set = await Settings.Get(ctx.Guild);
            if (set.Quotes.Count == 0) {
                await ctx.RespondAsync("You have no quotes!");
                return;
            }

            var builder = new Paginator(ctx.Client.GetInteractivity()) {
                Strings = set.Quotes.Select(q => q.Length > 200 ? q.Substring(0, 200) + "..." : q).ToList()
            };
            builder.SetGenericColor(ctx.Member.Color);
            builder.SetGenericText("Here are your quotes:");
            builder.Users.Add(ctx.Member.Id);

            await builder.Display(ctx.Channel);
        }

        [Command("edit"), Description("Edits a quote you specify."), RequireUserPermissions(DSharpPlus.Permissions.ManageMessages)]
        public async Task Edit(CommandContext ctx, 
            [Description("The number of the quote you wish to edit."), Required] int QuoteNumber, 
            [RemainingText, Description("The new contents of the quote.")] string Quote) {
            var set = await Settings.Get(ctx.Guild);
            var toGet = QuoteNumber - 1;
            if (set.Quotes.Count == 0) {
                await ctx.RespondAsync("You have no quotes!");
                return;
            }

            if (set.Quotes.Count < toGet || toGet < 0) {
                //do nothing rn im lazy
                return;
            }

            if (string.IsNullOrEmpty(Quote)) {
                //do nothing for now, requires questionnaire system to be ported over.
                await ctx.RespondAsync("blech ew gross unfinished things");
                return;
            }

            set.Quotes[toGet] = Quote;
            await set.Save();
            await ctx.RespondAsync($"Successfully edited quote `{QuoteNumber}`.");
        }

        [Command("search"), Description("Searches a query from within your quotes.")]
        public async Task Search(CommandContext ctx, [RemainingText, Description("The query to search for.")] string Query) {
            var set = await Settings.Get(ctx.Guild);
            if (set.Quotes.Count == 0) {
                await ctx.RespondAsync("You have no quotes!");
                return;
            }
            if (string.IsNullOrEmpty(Query)) {
                await ctx.RespondAsync("No search specified.");
                return;
            }
            var search = new List<string>();
            var reg = $"(?i).*({Query}).*";

            for (int i = 0; i < set.Quotes.Count; i++) {
                if (Regex.IsMatch(set.Quotes[i], reg)) search.Add($"`{i}.` {set.Quotes[i]}");
            }

            if (search.Count == 0) {
                await ctx.RespondAsync($"No matches found for: {Query}");
                return;
            }

            var builder = new Paginator(ctx.Client.GetInteractivity());

            builder.SetGenericText($"{search.Count} matches found for: {Query}.");
            builder.SetGenericColor(ctx.Member.Color);
            builder.Users.Add(ctx.Member.Id);
            builder.Strings = search;
            builder.NumberedItems = false;

            await builder.Display(ctx.Channel);
        }

        [Command("dump"), Description("Dumps all quotes into a text file."), RequireUserPermissions(DSharpPlus.Permissions.ManageMessages)]
        public async Task Dump(CommandContext ctx) {
            var set = await Settings.Get(ctx.Guild);
            if (set.Quotes.Count == 0) {
                await ctx.RespondAsync("You have no quotes!");
                return;
            }

            var dumper = new StringBuilder();
            for (int i = 0; i < set.Quotes.Count; i++) {
                dumper.AppendLine($"{i + 1}. {set.Quotes[i]}");
            }
            var dump = Encoding.UTF8.GetBytes(dumper.ToString());
            var stream = new MemoryStream(dump);
            await ctx.RespondWithFileAsync("quotes.txt", stream, "Dump successful, download below:");
            await stream.DisposeAsync();

        }
    }
}
