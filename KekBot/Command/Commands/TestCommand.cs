using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using ImageMagick;
using KekBot.Menu;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace KekBot {
    public class TestCommand : BaseCommandModule {
        [Command("test"), Description("example test command")]
        public async Task Test(CommandContext ctx) {
            /*var pag = new Paginator(ctx.Client.GetInteractivity());
            for (int i = 0; i < 20; i++) {
                pag.strings.Add($"test {i + 1}");
            }
            pag.itemsPerPage = 5;
            pag.users.Add(ctx.Member.Id);
            pag.finalAction = async m => await m.DeleteAllReactionsAsync();
            pag.timeout = TimeSpan.FromSeconds(30);

            await pag.Display(ctx.Channel);*/
            
            
            
            var pag = new EmbedPaginator(ctx.Client.GetInteractivity());
            for (int i = 0; i < 5; i++) {
                var builder = new DiscordEmbedBuilder();
                builder.Title = $"{i}";
                pag.Embeds.Add(builder.Build());
            }
            pag.Users.Add(ctx.Member.Id);
            pag.ShowPageNumbers = true;
            pag.Timeout = TimeSpan.FromSeconds(30);
            pag.FinalAction = async m => await m.DeleteAllReactionsAsync();

            await pag.Display(ctx.Channel);

            /*await ctx.TriggerTypingAsync();

            WebClient client = new WebClient();
            Stream stream = await client.OpenReadTaskAsync(new Uri(ctx.User.AvatarUrl));
            IMagickImage ava = new MagickImage(stream);
            Stream streamm = new MemoryStream();
            IMagickImage test2 = new MagickImage(MagickColor.FromRgba(255, 0, 0, 255), ava.Width, ava.Height);
            test2.Format = MagickFormat.Png64;
            test2.Composite(ava, 50, 50, CompositeOperator.SrcOver);
            await ctx.RespondWithFileAsync("test.png", new MemoryStream(test2.ToByteArray()), "test");*/
        }

    }
}
