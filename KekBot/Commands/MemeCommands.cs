using ImageMagick;
using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using KekBot.Attributes;
using KekBot.Utils;

namespace KekBot.Commands {
    public class MemeCommands : BaseCommandModule {

        private readonly Randumb Random = Randumb.Instance;

        [Command("brave"), Description("I'm a brave boy!"), Category(Category.Meme), Priority(0)]
        async Task Brave(CommandContext ctx) {
            //todo image searching for memes
            if (ctx.Message.Attachments.Count == 0) return;

            await ctx.TriggerTypingAsync();
            var a = ctx.Message.Attachments[0];

            await GenerateBrave(ctx, new Uri(a.Url));
        }

        [Command("brave"), Priority(1)]
        async Task Brave(CommandContext ctx, [Description("The link to an image to use for this meme.")] Uri Image) {
            await ctx.TriggerTypingAsync();

            await GenerateBrave(ctx, Image);
        }

        private async Task GenerateBrave(CommandContext ctx, Uri link) {
            using (var client = new WebClient()) {
                var template = new MagickImage("Resource/Files/memegen/brave.jpg");
                var _ = await client.OpenReadTaskAsync(link);
                var image = new MagickImage(_);

                var widthRatio = 892d / image.Width;
                var heightRatio = 1108d / image.Height;
                var ratio = Math.Min(widthRatio, heightRatio);

                var width = (int)(image.Width * ratio);
                var height = (int)(image.Height * ratio);

                var x = (892 / 2) - (width / 2);
                var y = (1108 / 2) - (height / 2);

                image.Resize(width, height);
                template.Composite(image, x, 1112 + y, CompositeOperator.SrcOver);


                var output = new MemoryStream(template.ToByteArray());
                await ctx.RespondWithFileAsync("test.png", output);
                template.Dispose();
                image.Dispose();
                output.Dispose();
            }
        }

        [Command("triggered"), Description("I'm T R I G G E R E D"), Category(Category.Meme), Priority(0)]
        async Task Triggered(CommandContext ctx) {
            await ctx.TriggerTypingAsync();

            await GenerateTriggered(ctx, new Uri(ctx.Member.AvatarUrl));
        }

        [Command("triggered"), Priority(1)]
        async Task Triggered(CommandContext ctx, [Description("The user to generate a triggered image from.")] DiscordMember Member) {
            await ctx.TriggerTypingAsync();

            await GenerateTriggered(ctx, new Uri(Member.AvatarUrl));
        }

        private async Task GenerateTriggered(CommandContext ctx, Uri image) {
            using (var client = new WebClient()) {
                var _ = await client.OpenReadTaskAsync(image);
                var ava = new MagickImage(_);
                ava.Resize(500, 500);
                var canvas = new MagickImage(MagickColor.FromRgba(0, 0, 0, 0), 500, 500);
                canvas.Format = MagickFormat.Gif;
                var triggered = new MagickImage("Resource/Files/memegen/triggered.png");
                var overlay = new MagickImage("Resource/Files/memegen/triggered_overlay.png");
                using (var gif = new MagickImageCollection()) {
                    for (int i = 0; i < 10; i++) {
                        gif.Add(canvas.Clone());
                        gif[i].AnimationDelay = 3;
                        gif[i].Composite(ava, Random.Next(-30, 30), Random.Next(-20, 20), CompositeOperator.SrcOver);
                        gif[i].Composite(triggered, Random.Next(-30, 30), 327 + Random.Next(-20, 20), CompositeOperator.SrcOver);
                        gif[i].Composite(overlay, CompositeOperator.SrcOver);
                        gif[i].GifDisposeMethod = GifDisposeMethod.Previous;
                    }

                    var settings = new QuantizeSettings {
                        Colors = 256
                    };
                    gif.Quantize(settings);
                    gif.Coalesce();

                    var stream = new MemoryStream();
                    gif.Write(stream);

                    await ctx.RespondWithFileAsync("triggered.gif", stream);
                    stream.Dispose();
                    ava.Dispose();
                }
                canvas.Dispose();
                triggered.Dispose();
                overlay.Dispose();
            }
        }

    }
}
