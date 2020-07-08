using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using KekBot.Attributes;
using RethinkDb.Driver.Ast;
using System;
using System.Runtime.Serialization;
using System.Threading.Tasks;

namespace KekBot.Commands {
    public class OwnerCommands : BaseCommandModule {
        [Command("sudo"), Description("Forces a command to be run as someone else."), RequireOwner, Aliases("s", "sud", "sudoooooooo")]
        [Priority(0)]
        async Task SudoCommand(CommandContext ctx, [Description("User to run the command as.")] DiscordMember member, [RemainingText, Description("The command to run (and its arguments)")] string command) {
            var cmd = ctx.CommandsNext.FindCommand(command, out var args);
            CommandContext fakectx = ctx.CommandsNext.CreateFakeContext(member, ctx.Channel, ctx.Message.Content, ctx.Prefix, cmd, args);
            await ctx.CommandsNext.ExecuteCommandAsync(fakectx);
            /*leftover code for image manip, ignore me
            await ctx.TriggerTypingAsync();

            WebClient client = new WebClient();
            Stream stream = await client.OpenReadTaskAsync(new Uri(ctx.User.AvatarUrl));
            IMagickImage ava = new MagickImage(stream);
            Stream streamm = new MemoryStream();
            IMagickImage test2 = new MagickImage(MagickColor.FromRgba(255, 0, 0, 255), ava.Width, ava.Height);
            test2.Format = MagickFormat.Png64;
            test2.Composite(ava, 50, 50, CompositeOperator.SrcOver);
            await ctx.RespondWithFileAsync("test.png", new MemoryStream(test2.ToByteArray()), "test");*/
        }

        [Command("sudo"), Priority(-1)]
        async Task SudoCommand(CommandContext ctx) {
            var cmd = ctx.CommandsNext.FindCommand("help sudo", out var args);
            CommandContext fakectx = ctx.CommandsNext.CreateFakeContext(ctx.Member, ctx.Channel, ctx.Message.Content, ctx.Prefix, cmd, args);
            await ctx.CommandsNext.ExecuteCommandAsync(fakectx);
        }

        [Command("rank"), Description("Gives a specified user a rank for moderating/editing KekBot."), RequireOwner]
        async Task GiveRank(CommandContext ctx, [Description("The rank to assign to a specified user.")] String ToGive, [RemainingText, Description("The user that will be assigned the rank.")] DiscordUser User) {
            Config config = await Config.Get();
            if (!Enum.TryParse<Rank>(ToGive, true, out var r)) {
                await ctx.RespondAsync("Invalid Rank.");
                return;
            }

            if (config.RankedUsers.ContainsKey(User.Id)) {
                if (config.RankedUsers[User.Id] == r) await ctx.RespondAsync("The specified user already has this rank.");
                else if (r == Rank.None) {
                    config.RankedUsers.Remove(User.Id);
                    config.Save();
                    await ctx.RespondAsync($"Rank removed from {User.Username}#{User.Discriminator}.");
                }
                else {
                    config.RankedUsers[User.Id] = r;
                    config.Save();
                    await ctx.RespondAsync($"{User.Username}#{User.Discriminator} has been assigned the rank: `{Enum.GetName(typeof(Rank), r)}`");
                }
            } else {
                if (r == Rank.None) {
                    await ctx.RespondAsync("This user already does not have a rank.");
                    return;
                }
                config.RankedUsers.Add(User.Id, r);
                config.Save();
                await ctx.RespondAsync($"{User.Username}#{User.Discriminator} has been assigned the rank: `{Enum.GetName(typeof(Rank), r)}`");
            }
        }
    }
}
