using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using ImageMagick;
using ImageMagick.Defines;
using KekBot.Menu;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;

namespace KekBot.Commands {
    public class TestCommand : BaseCommandModule {
        [Command("test"), Description("example test command")]
        public async Task Test(CommandContext ctx) {
            //await ctx.RespondAsync(DiscordEmoji.FromGuildEmote(ctx.Client, 403114999578361856ul).GetDiscordName());

            /*var pag = new Paginator(ctx.Client.GetInteractivity());
            for (int i = 0; i < 20; i++) {
                pag.strings.Add($"test {i + 1}");
            }
            pag.itemsPerPage = 5;
            pag.users.Add(ctx.Member.Id);
            pag.finalAction = async m => await m.DeleteAllReactionsAsync();
            pag.timeout = TimeSpan.FromSeconds(30);

            await pag.Display(ctx.Channel);*/



            /*var pag = new EmbedPaginator(ctx.Client.GetInteractivity());
            for (int i = 0; i < 5; i++) {
                var builder = new DiscordEmbedBuilder();
                builder.Title = $"{i}";
                pag.Embeds.Add(builder.Build());
            }
            pag.Users.Add(ctx.Member.Id);
            pag.ShowPageNumbers = true;
            pag.Timeout = TimeSpan.FromSeconds(30);
            pag.FinalAction = async m => await m.DeleteAllReactionsAsync();

            await pag.Display(ctx.Channel);*/

            await ctx.TriggerTypingAsync();

            using (var client = new WebClient()) {
                var _ = await client.OpenReadTaskAsync(new Uri(ctx.User.AvatarUrl));
                var ava = new MagickImage(_);
                using (var gif = new MagickImageCollection()) {
                    var test = new MagickImage(MagickColor.FromRgba(255, 0, 0, 255), ava.Width, ava.Height);
                    test.Format = MagickFormat.Gif;
                    gif.Add(test.Clone());
                    gif.Add(test.Clone());
                    gif[0].Composite(ava, 0, 0, CompositeOperator.SrcOver);
                    gif[0].AnimationDelay = 2;
                    gif[1].Composite(ava, 50, 50, CompositeOperator.SrcOver);
                    gif[1].AnimationDelay = 2;

                    var settings = new QuantizeSettings {
                        Colors = 256
                    };
                    gif.Quantize(settings);

                    var stream = new MemoryStream(gif.ToByteArray());

                    await ctx.RespondWithFileAsync("test.gif", stream, "test");
                    stream.Dispose();
                    ava.Dispose();
                    test.Dispose();
                }
            }
        }

    }
}
