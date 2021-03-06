﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using KekBot.Attributes;
using KekBot.Lib;
using KekBot.Menu;
using KekBot.Utils;
using RethinkDb.Driver.Ast;

namespace KekBot.Commands {
    class HelpCommand : BaseCommandModule {

        private const string tagline = "KekBot, your friendly meme based bot!";

        private CommandInfoList CommandInfo { get; }
        private FakeCommandsDictionary FakeCommands { get; }
        private KekBot Bot { get; }

        public HelpCommand(CommandInfoList commandInfo, FakeCommandsDictionary fakeCommands, KekBot Bot) {
            CommandInfo = commandInfo;
            FakeCommands = fakeCommands;
            this.Bot = Bot;
        }

        [Command("help"), Description("You're already here, aren't you?"), Category(Category.General)]
        async Task Help(
            CommandContext ctx,
            [RemainingText, Description("The command or category to look for.")] string query = ""
        ) {
            if (query.Length == 0) {
                //If no arguments were given, bring the commands list.
                await DisplayCategoryHelp(ctx);
            } else if (ctx.CommandsNext.FindCommand(query, out var _) is Command cmd) {
                //Print the command help, if the command has been found.
                await DisplayCommandHelp(ctx, CommandInfo.GetFor(cmd));
            } else if (FakeCommands.Contains(query)) {
                //Print the command help, if the (fake) command has been found.
                await DisplayCommandHelp(ctx, CommandInfo.GetFor(query));
            } else if (Enum.TryParse(query, true, out Category cat)) {
                //Command wasn't found, is it a category?
                await DisplayCategoryHelp(ctx, cat);
            } else {
                //the answer is no
                await ctx.RespondAsync("Command/Category not found.");
            }
        }

        private async Task DisplayCommandHelp(CommandContext ctx, ICommandInfo cmd) {
            //Setup the embed.
            var aliases = string.Join(", ", cmd.Aliases.Select(alias => $"`{alias}`"));
            var embed = new DiscordEmbedBuilder()
                .WithTitle($"`{cmd.Name}`" + (aliases.Length > 0 ? $" (or {aliases})" : ""))
                .WithDescription(cmd.Description)
                .WithAuthor(name: tagline, iconUrl: ctx.Client.CurrentUser.AvatarUrl);
            //Prepare ourselves for usage
            var usage = new StringBuilder();
            //Try the cast once.
            var group = cmd.Cmd as CommandGroup;
            //The total count of subcommands and overloads.
            var count = cmd.Overloads.Length + (group?.Children.Count ?? 0);

            //The following loop handles overloads.
            foreach (var ovrld in cmd.Overloads) {
                if (ovrld.Priority >= 0)
                    AppendOverload(ovrld);
            }

            //Do we have any subcommands?
            if (group != null) {
                //The following loop handles subcommands and their appropriate usage.
                foreach (var subcmd in group.Children) {
                    if (subcmd.Overloads.Count > 1 || subcmd is CommandGroup)
                        usage.AppendLine($"`{Bot.GetPrefix(ctx.Guild)}{subcmd.QualifiedName}`: Visit `help {subcmd.QualifiedName}` for more information.");
                    else
                        AppendOverload(new CommandOverloadInfo(subcmd.Overloads.Single()), subcmd);
                }
            }

            //I heard you like methods, so I put a method in your method.
            void AppendOverload(ICommandOverloadInfo ovrld, Command? subcmd = null) {
                var args = ovrld.Arguments.Where(arg => !arg.IsHidden);
                var ovrldHasArgs = args.Any();

                usage.Append("`");
                usage.Append(Bot.GetPrefix(ctx.Guild) + cmd.Cmd.QualifiedName);
                if (subcmd != null) usage.Append($" {subcmd.Name}");
                //Make sure we actually have arguments, otherwise don't bother adding a space for them.
                if (ovrldHasArgs) {
                    usage.Append(" ");
                    //We have arguments, let's print them.
                    usage.AppendJoin(" ", args.Select(arg => arg.IsOptional ? $"({arg.Name})" : $"[{arg.Name}]"));
                }
                usage.Append("`");

                if (subcmd != null) usage.Append($": {subcmd.Description}");
                usage.AppendLine();
                //Is the count of subcommands and overloads short?
                if (count <= 6) {
                    //Second argument loop for descriptions (if count is short)
                    if (ovrldHasArgs)
                        usage.AppendLines(args.Select(arg => $"`{arg.Name}`: {arg.Description}"));
                    usage.AppendLine();
                }
            }

            embed.AddField("Usage:", usage.ToString(), inline: false);
            await ctx.RespondAsync(embed: embed);
        }

        private async Task DisplayCategoryHelp(CommandContext ctx, Category? catOrAll = null) {
            var paginator = new EmbedPaginator(ctx.Client.GetInteractivity()) {
                ShowPageNumbers = true
            };
            paginator.Users.Add(ctx.Member.Id);

            paginator.Embeds.AddRange(catOrAll is Category cat
                ? GetCategoryPages(ctx, cat)
                : Enum.GetValues(typeof(Category)).Cast<Category>().SelectMany(cat => GetCategoryPages(ctx, cat)));

            Util.Assert(paginator.Embeds.Count > 0, elsePanicWith: "no commands???");

            await paginator.Display(ctx.Channel);
        }

        private IEnumerable<DiscordEmbed> GetCategoryPages(CommandContext ctx, Category cat) {
            var prefix = Bot.GetPrefix(ctx.Guild);
            var cmds = CommandInfo
                .Where(c => c.Category == cat)
                .OrderBy(c => c.Name)
                .Distinct()
                .ToList();
            return Util.Range(end: cmds.Count, step: 10)
                .Select(i => new DiscordEmbedBuilder()
                    .WithTitle(Enum.GetName(typeof(Category), cat))
                    .WithDescription(string.Join("\n", cmds
                        .GetRange(i, Math.Min(10, cmds.Count - i))
                        .Select(c => $"{prefix}{c.Name} - {c.Description}")))
                    .WithAuthor(tagline, iconUrl: ctx.Client.CurrentUser.AvatarUrl)
                    .WithFooter("KekBot v2.0")
                    .Build());
        }

    }
}
