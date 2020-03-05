using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using KekBot.Attributes;
using KekBot.Menu;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KekBot.Command.Commands
{

    class HelpCommand : BaseCommandModule
    {
        private const string tagline = "KekBot, your friendly meme based bot!";

        [Command("help"), Description("You're already here, aren't you?"), Category(Category.General)]
        public async Task Help(CommandContext ctx, [RemainingText, Description("The command or category to look for.")] string query = "")
        {
            //If no arguments were given, bring the commands list.
            if (query.Length == 0)
            {
                var paginator = new EmbedPaginator(ctx.Client.GetInteractivity());
                paginator.FinalAction = async m => await m.DeleteAllReactionsAsync();
                paginator.ShowPageNumbers = true;
                paginator.users.Add(ctx.Member.Id);

                var cats = Enum.GetValues(typeof(Category)).Cast<Category>();
                foreach (var cat in cats)
                {
                    PrintCommandsInCategory(ctx, paginator, cat);
                }

                await paginator.Display(ctx.Channel);
                return;
            }


            //Find the command
            var cmd = ctx.CommandsNext.FindCommand(query, out var args);
            //Was the command found?
            if (cmd == null)
            {
                if (Enum.GetNames(typeof(Category)).Select(s => s.ToLower()).Contains(query.ToLower()))
                {
                    var paginator = new EmbedPaginator(ctx.Client.GetInteractivity());
                    paginator.users.Add(ctx.Member.Id);
                    paginator.ShowPageNumbers = true;

                    var cat = (Category)Enum.Parse(typeof(Category), query, true);
                    PrintCommandsInCategory(ctx, paginator, cat);
                    await paginator.Display(ctx.Channel);
                }
                else
                {
                    await ctx.RespondAsync("Command/Category not found.");
                }
                return;
            }


            //Setup the embed.
            var embed = new DiscordEmbedBuilder();
            embed.WithAuthor(tagline, null, ctx.Client.CurrentUser.AvatarUrl);
            embed.Author.Name = tagline;
            //Prepare ourselves in case the command has aliases
            var aliases = new StringBuilder();
            for (int i = 0; i < cmd.Aliases.Count; i++)
            {
                string alias = (string)cmd.Aliases[i];
                aliases.Append($"`{alias}`");
                if (i < cmd.Aliases.Count - 1) aliases.Append(", ");
            }
            embed.Title = $"`{cmd.Name}`" + (cmd.Aliases.Count > 0 ? $" (or {aliases.ToString()})" : "");
            embed.Description = cmd.Description;
            //Prepare ourselves for usage
            var usage = new StringBuilder();
            //The total count of subcommands and overloads.
            var count = cmd.Overloads.Count;

            //Do we have any subcommands?
            if (cmd is CommandGroup)
            {
                var g = cmd as CommandGroup;
                count += g.Children.Count;
                //The following loop handles subcommands and their appropriate usage.
                foreach (var subcmd in g.Children)
                {
                    if (subcmd.Overloads.Count > 1 || subcmd is CommandGroup)
                    {
                        usage.Append($"`{cmd.Name} {subcmd.Name}`: Visit `help {cmd.Name} {subcmd.Name}` for more information.");
                    }
                    else
                    {
                        usage.Append($"`{cmd.Name} {subcmd.Name}");
                        var ovrld = subcmd.Overloads[0];
                        //Make sure we actually have arguments, otherwise don't bother adding a space for them.
                        if (ovrld.Arguments.Count < 1)
                        {
                            usage.AppendLine($"`: {subcmd.Description}");
                            if (count <= 6) usage.AppendLine();
                            continue;
                        }
                        //We have arguments, let's print them.
                        usage.Append(" ");
                        for (int i = 0; i < ovrld.Arguments.Count; i++)
                        {
                            CommandArgument arg = ovrld.Arguments[i];
                            usage.Append(arg.IsOptional ? $"({arg.Name})" : $"[{arg.Name}]");
                            if (i < ovrld.Arguments.Count - 1) usage.Append(" ");
                        }
                        usage.AppendLine($"`: {subcmd.Description}");
                        //Second argument loop for descriptions (if overloads <= 5)
                        if (count <= 6)
                        {
                            foreach (var arg in ovrld.Arguments)
                            {
                                usage.AppendLine($"`{arg.Name}`: {arg.Description}");
                            }
                            usage.AppendLine();
                        }
                        //usage.AppendLine(count <= 6 ? "" : "`");
                    }
                }
            }

            //The following loop handles overloads.
            foreach (var ovrld in cmd.Overloads)
            {
                if (ovrld.Priority < 0) continue;
                usage.Append($"`{cmd.Name}");
                //Make sure we actually have arguments, otherwise don't bother adding a space for them.
                if (ovrld.Arguments.Count < 1)
                {
                    usage.AppendLine("`");
                    if (count <= 6) usage.AppendLine();
                    continue;
                }

                //We have arguments, let's print them.
                usage.Append(" ");
                //First argument loop for usage
                for (int i = 0; i < ovrld.Arguments.Count; i++)
                {
                    CommandArgument arg = ovrld.Arguments[i];
                    usage.Append(arg.IsOptional ? $"({arg.Name})" : $"[{arg.Name}]");
                    if (i < ovrld.Arguments.Count - 1) usage.Append(" ");
                }
                //Second argument loop for descriptions (if overloads <= 5)
                usage.AppendLine("`");
                if (count <= 6)
                {
                    foreach (var arg in ovrld.Arguments)
                    {
                        usage.AppendLine($"`{arg.Name}`: {arg.Description}");
                    }
                    usage.AppendLine();
                }
            }
            embed.AddField("Usage:", usage.ToString(), false);
            await ctx.RespondAsync(embed: embed);
        }

        private static void PrintCommandsInCategory(CommandContext ctx, EmbedPaginator paginator, Category cat)
        {
            var cmds = ctx.CommandsNext.RegisteredCommands.Values.Where(c => c.GetCategory().Equals(cat)).OrderBy(c => c.Name).Distinct();
            for (int i = 0; i < cmds.Count(); i += 10)
            {
                var page = cmds.ToList().GetRange(i, Math.Min(i + 10, cmds.Count()));
                DiscordEmbedBuilder builder = new DiscordEmbedBuilder();
                builder.Title = Enum.GetName(typeof(Category), cat);
                builder.Description = string.Join("\n", page.Select(c => $"{c.Name} - {c.Description}"));
                builder.WithAuthor(tagline, iconUrl: ctx.Client.CurrentUser.AvatarUrl);
                builder.WithFooter("KekBot v2.0");
                paginator.Embeds.Add(builder.Build());
            }
        }
    }
}
