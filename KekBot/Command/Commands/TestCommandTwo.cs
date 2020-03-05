using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using KekBot.Attributes;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace KekBot.Command.Commands {
    [Group("testtwo"), Description("a collection of test commands")]
    class TestCommandTwo : BaseCommandModule {
        [Command("one"), Description("example test command"), Category(Category.Meme)]
        public async Task Test(CommandContext ctx) {
            await ctx.RespondAsync($"{ctx.Message.Author.Mention} you smell");
            /*
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

        [Command("two"), Description("hell if i know")]
        public async Task Two(CommandContext ctx, [Description("just a random string to use")] String penis) {
            await ctx.RespondAsync($"You typed: {penis}");
        }
    }
}
