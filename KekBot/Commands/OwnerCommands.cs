using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using ImageMagick;
using KekBot.Attributes;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using CategoryAttribute = KekBot.Attributes.CategoryAttribute;
using DescriptionAttribute = DSharpPlus.CommandsNext.Attributes.DescriptionAttribute;

namespace KekBot.Commands {
    public class OwnerCommands : BaseCommandModule {
        [Command("sudo"), Description("Forces a command to be run as someone else."), RequireOwner, Aliases("s", "sud", "sudoooooooo"), Category(Category.Fun)]
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



    }
}
