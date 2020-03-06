using System;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
// Conflicts with KekBot.Command otherwise
using Cmd = DSharpPlus.CommandsNext.Command;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using KekBot.Attributes;
using KekBot.Menu;
using KekBot.Util;

namespace KekBot.Command.Commands {
    class HelpCommand : BaseCommandModule {

        private const string tagline = "KekBot, your friendly meme based bot!";

        [Command("help"), Description("You're already here, aren't you?"), Category(Category.General)]
        public async Task Help(
            CommandContext ctx,
            [RemainingText, Description("The command or category to look for.")] string query = ""
        ) {
            //If no arguments were given, bring the commands list.
            if (query.Length == 0) {
                var paginator = new EmbedPaginator(ctx.Client.GetInteractivity()) {
                    FinalAction = async m => await m.DeleteAllReactionsAsync(),
                    ShowPageNumbers = true
                };
                paginator.users.Add(ctx.Member.Id);

                var cats = Enum.GetValues(typeof(Category)).Cast<Category>();
                foreach (var c in cats) {
                    PrintCommandsInCategory(ctx, paginator, c);
                }

                await paginator.Display(ctx.Channel);
                return;
            }


            //Print the command help, if the command has been found.
            if (ctx.CommandsNext.FindCommand(query, out var _) is Cmd cmd) {
                await DisplayCommandHelp(ctx, cmd);
                return;
            }
            
            //Command wasn't found, is it a category?
            if (Enum.TryParse(query, true, out Category cat)) {
                var paginator = new EmbedPaginator(ctx.Client.GetInteractivity());
                paginator.users.Add(ctx.Member.Id);
                paginator.ShowPageNumbers = true;

                PrintCommandsInCategory(ctx, paginator, cat);
                await paginator.Display(ctx.Channel);
                return;
            }
            
            //the answer is no
            await ctx.RespondAsync("Command/Category not found.");
        }

        private static void PrintCommandsInCategory(CommandContext ctx, EmbedPaginator paginator, Category cat) {
            var cmds = ctx.CommandsNext.RegisteredCommands.Values
                .Where(c => c.GetCategory() == cat)
                .OrderBy(c => c.Name)
                .Distinct();
            for (int i = 0; i < cmds.Count(); i += 10) {
                var page = cmds.ToList().GetRange(i, Math.Min(i + 10, cmds.Count()));
                var builder = new DiscordEmbedBuilder {
                    Title = Enum.GetName(typeof(Category), cat),
                    Description = string.Join("\n", page.Select(c => $"{c.Name} - {c.Description}"))
                };
                builder
                    .WithAuthor(tagline, iconUrl: ctx.Client.CurrentUser.AvatarUrl)
                    .WithFooter("KekBot v2.0");
                paginator.Embeds.Add(builder.Build());
            }
        }

        private static async Task DisplayCommandHelp(CommandContext ctx, Cmd cmd) {
            //Setup the embed.
            var aliases = string.Join(", ", cmd.Aliases.Select(alias => $"`{alias}`"));
            var embed = new DiscordEmbedBuilder
            {
                Title = $"`{cmd.Name}`" + (aliases.Length > 0 ? $" (or {aliases})" : ""),
                Description = cmd.Description
            };
            embed.WithAuthor(name: tagline, iconUrl: ctx.Client.CurrentUser.AvatarUrl);
            //Prepare ourselves for usage
            var usage = new StringBuilder();
            //The total count of subcommands and overloads.
            var count = cmd.Overloads.Count;

            //Do we have any subcommands?
            if (cmd is CommandGroup group) {
                count += group.Children.Count;
                //The following loop handles subcommands and their appropriate usage.
                foreach (var subcmd in group.Children) {
                    if (subcmd.Overloads.Count > 1 || subcmd is CommandGroup)
                        usage.Append($"`{cmd.Name} {subcmd.Name}`: Visit `help {cmd.Name} {subcmd.Name}` for more information.");
                    else
                        AppendOverload(subcmd.Overloads.Single(), subcmd);
                }
            }

            //The following loop handles overloads.
            foreach (var ovrld in cmd.Overloads) {
                if (ovrld.Priority >= 0)
                    AppendOverload(ovrld);
            }

            //I heard you like methods, so I put a method in your method.
            void AppendOverload(CommandOverload ovrld, Cmd? subcmd = null) {
                var ovrldHasArgs = ovrld.Arguments.Count > 0;

                usage.Append("`");
                usage.Append(cmd.Name);
                if (subcmd != null) usage.Append($" {subcmd.Name}");
                //Make sure we actually have arguments, otherwise don't bother adding a space for them.
                if (ovrldHasArgs) {
                    usage.Append(" ");
                    //We have arguments, let's print them.
                    usage.AppendJoin(" ", ovrld.Arguments.Select(arg => arg.IsOptional ? $"({arg.Name})" : $"[{arg.Name}]"));
                }
                usage.Append("`");

                if (subcmd != null) usage.Append($": {subcmd.Description}");
                usage.AppendLine();
                //Is the count of subcommands and overloads short?
                if (count <= 6) {
                    //Second argument loop for descriptions (if count is short)
                    if (ovrldHasArgs)
                        usage.AppendLines(ovrld.Arguments.Select(arg => $"`{arg.Name}`: {arg.Description}"));
                    usage.AppendLine();
                }
            }

            embed.AddField("Usage:", usage.ToString(), inline: false);
            await ctx.RespondAsync(embed: embed);
        }

    }
}
