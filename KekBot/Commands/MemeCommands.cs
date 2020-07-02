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
using KekBot.Arguments;
using RethinkDb.Driver.Ast;
using DSharpPlus.CommandsNext.Converters;
using DSharpPlus;

namespace KekBot.Commands {
    public class MemeCommands : BaseCommandModule {

        private readonly Randumb Random = Randumb.Instance;

        [Command("brave"), Description("I'm a brave boy!"), Category(Category.Meme), Priority(0)]
        async Task Brave(
            CommandContext ctx,
            [RemainingText,
            Description("The link to an image to use for this meme. If none given, KekBot will search your command for an attachment. If none found, it'll search message history for an image to use.")]
            Uri? Image = null) {
            await ctx.TriggerTypingAsync();

            Uri? uri = Image;
            //Checks if "Image" was null.
            if (uri == null) {
                if (ctx.Message.Attachments.Count > 0) {
                    uri = new Uri(ctx.Message.Attachments[0].Url);
                } else {
                    uri = await HuntForImage(ctx);
                }
            }
            //Checks if the search failed.
            if (uri == null) {
                await ctx.RespondAsync("No image found.");
                return;
            }



            using (var client = new WebClient()) {
                using var template = new MagickImage("Resource/Files/memegen/brave.jpg");
                using var _ = await client.OpenReadTaskAsync(uri);
                using var image = new MagickImage(_);

                var widthRatio = 892d / image.Width;
                var heightRatio = 1108d / image.Height;
                var ratio = Math.Min(widthRatio, heightRatio);

                var width = (int)(image.Width * ratio);
                var height = (int)(image.Height * ratio);

                var x = (892 / 2) - (width / 2);
                var y = (1108 / 2) - (height / 2);

                image.Resize(width, height);
                template.Composite(image, x, 1112 + y, CompositeOperator.SrcOver);


                using var output = new MemoryStream(template.ToByteArray());
                await ctx.RespondWithFileAsync("test.png", output);
            }
        }

        [Command("triggered"), Description("I'm T R I G G E R E D"), Category(Category.Meme)]
        async Task Triggered(CommandContext ctx, [RemainingText, Description("The user to generate the image from. (If none given, it uses you.)")] DiscordMember? Member = null) {
            await ctx.TriggerTypingAsync();

            var m = Member ?? ctx.Member;

            using (var client = new WebClient()) {
                using var _ = await client.OpenReadTaskAsync(m.AvatarUrl);
                using var ava = new MagickImage(_);
                ava.Resize(500, 500);
                using var canvas = new MagickImage(MagickColor.FromRgba(0, 0, 0, 0), 500, 500);
                canvas.Format = MagickFormat.Gif;
                using var triggered = new MagickImage("Resource/Files/memegen/triggered.png");
                using var overlay = new MagickImage("Resource/Files/memegen/triggered_overlay.png");
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

                    using var stream = new MemoryStream(gif.ToByteArray());

                    await ctx.RespondWithFileAsync("triggered.gif", stream);
                }
            }
        }

        [Command("doubt"), Description("Displays your doubt."), Category(Category.Meme)]
        async Task Doubt(CommandContext ctx) {
            await ctx.TriggerTypingAsync();

            using (var client = new WebClient()) {
                using var _ = await client.OpenReadTaskAsync(ctx.Member.AvatarUrl);
                using var ava = new MagickImage(_);
                using var template = new MagickImage("Resource/Files/memegen/doubt.png");
                ava.Resize(709, 709);
                template.Composite(ava, 0, 0, CompositeOperator.SrcOver);

                using var output = new MemoryStream(template.ToByteArray());

                await ctx.RespondWithFileAsync("doubt.png", output);
            }
        }

        [Command("erase"), Description("For really big mistakes."), Category(Category.Meme)]
        async Task Erase(CommandContext ctx, [Description("The user to generate the image from. (If none given, it uses you.)")] DiscordMember? Member = null) {
            await ctx.TriggerTypingAsync();

            var m = Member ?? ctx.Member;

            using (var client = new WebClient()) {
                using var _ = await client.OpenReadTaskAsync(m.AvatarUrl);
                using var ava = new MagickImage(_);
                using var template = new MagickImage("Resource/Files/memegen/mistake_template.png");
                ava.Resize(270, 270);
                template.Composite(ava, 368, 375, CompositeOperator.SrcOver);


                using var output = new MemoryStream(template.ToByteArray());

                await ctx.RespondWithFileAsync("erase.png", output);
            }
        }

        [Command("garage"), Description("Show your friends what's in your garage!"), Category(Category.Meme)]
        async Task Garage(CommandContext ctx,
            [Description("The link to an image to use for this meme. If none given, KekBot will search your command for an attachment. If none found, it'll search message history for an image to use.")]
            Uri? Image = null) {
            await ctx.TriggerTypingAsync();

            Uri? uri = Image;
            //Checks if "Image" was null.
            if (uri == null) {
                if (ctx.Message.Attachments.Count > 0) {
                    uri = new Uri(ctx.Message.Attachments[0].Url);
                } else {
                    uri = await HuntForImage(ctx);
                }
            }
            //Checks if the search failed.
            if (uri == null) {
                await ctx.RespondAsync("No image found.");
                return;
            }

            using (var client = new WebClient()) {
                using var _ = await client.OpenReadTaskAsync(uri);
                using var image = new MagickImage(_);
                using var template = new MagickImage("Resource/Files/memegen/garage.png");

                var widthRatio = 640d / image.Width;
                var heightRatio = 297d / image.Height;
                var ratio = Math.Min(widthRatio, heightRatio);

                var width = (int)(image.Width * ratio);
                var height = (int)(image.Height * ratio);

                var x = (640 / 2) - (width / 2);
                var y = (297 / 2) - (height / 2);

                image.Resize(width, height);
                template.Composite(image, x, 282 + y, CompositeOperator.DstOver);


                using var output = new MemoryStream(template.ToByteArray());

                await ctx.RespondWithFileAsync("garage.png", output);
            }
        }

        [Command("gril"), Description("Shows a topless gril."), Category(Category.Meme)]
        async Task Gril(CommandContext ctx, [HiddenParam, RemainingText] FlagArgs args) {
            await ctx.TriggerTypingAsync();

            var reboot = args.ParseBool("reboot") ?? false;

            await ctx.RespondWithFileAsync($"Resource/Files/memegen/topless_grill{(reboot ? "-reboot" : "")}.png");
        }

        [Command("johnny"), Description("HEREEEE'S JOHNNY!"), Category(Category.Meme)]
        async Task Johnny(CommandContext ctx, [RemainingText, Description("The user to target for the image.")] DiscordMember Member) {
            await ctx.TriggerTypingAsync();

            using (var client = new WebClient()) {
                using var _ = await client.OpenReadTaskAsync(ctx.Member.AvatarUrl);
                using var user = new MagickImage(_);
                using var __ = await client.OpenReadTaskAsync(Member.AvatarUrl);
                using var target = new MagickImage(__);
                using var template = new MagickImage("Resource/Files/memegen/johnny_template.png");

                user.Resize(283, 283);
                target.Resize(81, 71);

                template.Composite(user, 111, 218, CompositeOperator.DstOver);
                template.Composite(target, 250, -8, CompositeOperator.SrcOver);

                using var output = new MemoryStream(template.ToByteArray());

                await ctx.RespondWithFileAsync("jahnny.png", output);
            }
        }

        [Command("doorkick"), Description("WHAT DID I JUST WALK INTO"), Category(Category.Meme)]
        async Task DoorKick(CommandContext ctx,
            [Description("The link to an image to use for this meme. If none given, KekBot will search your command for an attachment. If none found, it'll search message history for an image to use.")]
            Uri? Image = null) {
            await ctx.TriggerTypingAsync();

            Uri? uri = Image;
            //Checks if "Image" was null.
            if (uri == null) {
                if (ctx.Message.Attachments.Count > 0) {
                    uri = new Uri(ctx.Message.Attachments[0].Url);
                } else {
                    uri = await HuntForImage(ctx);
                }
            }
            //Checks if the search failed.
            if (uri == null) {
                await ctx.RespondAsync("No image found.");
                return;
            }

            using (var client = new WebClient()) {
                using var _ = await client.OpenReadTaskAsync(uri);
                using var image = new MagickImage(_);
                using var template = new MagickImage("Resource/Files/memegen/door.png");

                var widthRatio = 338d / image.Width;
                var heightRatio = 466d / image.Height;
                var ratio = Math.Min(widthRatio, heightRatio);

                var width = (int)(image.Width * ratio);
                var height = (int)(image.Height * ratio);

                var x = (338 / 2) - (width / 2);
                var y = (466 / 2) - (height / 2);

                image.Resize(width, height);
                template.Composite(image, 326 + x, 7 + y, CompositeOperator.DstOver);


                using var output = new MemoryStream(template.ToByteArray());

                await ctx.RespondWithFileAsync("NOPE_NVM.png", output);
            }
        }

        [Command("gaybabyjail"), Description("Sends the target of your choosing to gay baby jail."), Aliases("gbj"), Category(Category.Meme)]
        async Task GBJ(CommandContext ctx, [RemainingText, Description("The user to target for the image.")] DiscordMember Member) {
            await ctx.TriggerTypingAsync();

            using (var client = new WebClient()) {
                using var _ = await client.OpenReadTaskAsync(Member.AvatarUrl);
                using var target = new MagickImage(_) {
                    ColorSpace = ColorSpace.Gray
                };
                target.Resize(430, 430);
                using var gbj = new MagickImage("Resource/Files/memegen/gbj.png");

                gbj.Composite(target, 109, 169, CompositeOperator.DstOver);

                using var output = new MemoryStream(gbj.ToByteArray());

                await ctx.RespondWithFileAsync("gbj.png", output);
            }
        }

        [Command("luigithumb"), Aliases("luigi"), Description("Gives an image Luigi's approval."), Category(Category.Meme)]
        async Task LuigiThumb(CommandContext ctx,
            [Description("The link to an image to use for this meme. If none given, KekBot will search your command for an attachment. If none found, it'll search message history for an image to use.")]
            Uri? Image = null) {
            await ctx.TriggerTypingAsync();

            Uri? uri = Image;
            //Checks if "Image" was null.
            if (uri == null) {
                if (ctx.Message.Attachments.Count > 0) {
                    uri = new Uri(ctx.Message.Attachments[0].Url);
                } else {
                    uri = await HuntForImage(ctx);
                }
            }
            //Checks if the search failed.
            if (uri == null) {
                await ctx.RespondAsync("No image found.");
                return;
            }

            using (var client = new WebClient()) {
                using var _ = await client.OpenReadTaskAsync(uri);
                using var image = new MagickImage(_);
                using var luigi = new MagickImage("Resource/Files/memegen/LuigiThumb.png");

                luigi.Resize((int)(image.Width * .50), (int)(image.Height * .50));
                image.Composite(luigi, image.Width - luigi.Width, image.Height - luigi.Height, CompositeOperator.SrcOver);

                using var output = new MemoryStream(image.ToByteArray());

                await ctx.RespondWithFileAsync("loogi.png", output);
            }
        }

        [Command("magik"), Description("Also known as the Content Awareness Scale."), ExtendedDescription("Huh? What do you mean NotSoBot didn't die? You mean this tribute was for nothing? Wack.")]
        [Category(Category.Meme)]
        async Task Magik(CommandContext ctx,
            [Description("The link to an image to use for this meme. If none given, KekBot will search your command for an attachment. If none found, it'll search message history for an image to use.")]
            Uri? Image = null) {
            await ctx.TriggerTypingAsync();

            Uri? uri = Image;
            //Checks if "Image" was null.
            if (uri == null) {
                if (ctx.Message.Attachments.Count > 0) {
                    uri = new Uri(ctx.Message.Attachments[0].Url);
                } else {
                    uri = await HuntForImage(ctx);
                }
            }
            //Checks if the search failed.
            if (uri == null) {
                await ctx.RespondAsync("No image found.");
                return;
            }

            using (var client = new WebClient()) {
                using var _ = await client.OpenReadTaskAsync(uri);
                using var image = new MagickImage(_);

                image.Resize(800, 800);
                image.LiquidRescale(400, 400);
                image.LiquidRescale(1200, 1200);

                using var output = new MemoryStream(image.ToByteArray());

                await ctx.RespondWithFileAsync("magik.png", output);
            }
        }

        [Command("poster"), Description("If only I had a Krabby Patty poster instead..."), Category(Category.Meme)]
        async Task Poster(CommandContext ctx,
            [Description("The link to an image to use for this meme. If none given, KekBot will search your command for an attachment. If none found, it'll search message history for an image to use.")]
            Uri? Image = null) {
            await ctx.TriggerTypingAsync();

            Uri? uri = Image;
            //Checks if "Image" was null.
            if (uri == null) {
                if (ctx.Message.Attachments.Count > 0) {
                    uri = new Uri(ctx.Message.Attachments[0].Url);
                } else {
                    uri = await HuntForImage(ctx);
                }
            }
            //Checks if the search failed.
            if (uri == null) {
                await ctx.RespondAsync("No image found.");
                return;
            }

            using (var client = new WebClient()) {
                using var _ = await client.OpenReadTaskAsync(uri);
                using var image = new MagickImage(_);
                using var template = new MagickImage("Resource/Files/memegen/lick.png");


                var widthRatio = 309d / image.Width;
                var heightRatio = 225d / image.Height;
                var ratio = Math.Min(widthRatio, heightRatio);

                var width = (int)(image.Width * ratio);
                var height = (int)(image.Height * ratio);

                var x = (309 / 2) - (width / 2);
                var y = (225 / 2) - (height / 2);

                image.Resize(width, height);
                image.BackgroundColor = MagickColors.White;
                image.VirtualPixelMethod = VirtualPixelMethod.Background;
                var settings = new DistortSettings() {
                    Viewport = new MagickGeometry(template.Width, template.Height)
                };
                //Distort the image to match the perspective of the template.
                //Pattern: (Src TL X, Src TL Y, Dest TL X, Dest TL Y, Src TR X, Src TR Y, Dest TR X, Dest TR Y, Src BR X, Src BR Y, Dest BR X, Dest BR Y, Src BL X, Src BL Y, Dest BR X, Dest BR Y)
                image.Distort(DistortMethod.Perspective, settings, 0, 0, 291 + x, 0 + y, image.Width, 0, 599 - x, 0 + y, image.Width, image.Height, 599 - x, 253 - y, 0, image.Height, 291 + x, 224 - y);
                template.Composite(image, CompositeOperator.DstOver);
                using var output = new MemoryStream(template.ToByteArray());

                await ctx.RespondWithFileAsync("magik.png", output);
            }
        }

        [Command("longlive"), Description("LONG LIVE THE KING!"), Category(Category.Meme)]
        async Task LongLive(CommandContext ctx, [RemainingText, Description("The user to target for the image.")] DiscordMember Member) {
            await ctx.TriggerTypingAsync();

            using (var client = new WebClient()) {
                using var _ = await client.OpenReadTaskAsync(ctx.Member.AvatarUrl);
                using var user = new MagickImage(_);
                using var __ = await client.OpenReadTaskAsync(Member.AvatarUrl);
                using var target = new MagickImage(__);
                using var template = new MagickImage("Resource/Files/memegen/longlivetheking_template.png");

                user.Resize(479, 479);
                target.Resize(442, 442);

                using var bg = new MagickImage(MagickColors.White, 479, 479);
                template.Composite(bg, 1026, 42, CompositeOperator.SrcOver);
                template.Composite(user, 1026, 42, CompositeOperator.SrcOver);
                bg.Resize(442, 442);
                template.Composite(bg, 503, 558, CompositeOperator.SrcOver);
                template.Composite(target, 503, 558, CompositeOperator.SrcOver);

                using var output = new MemoryStream(template.ToByteArray());

                await ctx.RespondWithFileAsync("theking.png", output);
            }
        }

        [Command("notallowed"), Description("Huh. I wonder who that's for?"), Category(Category.Meme)]
        async Task NotAllowed(CommandContext ctx, [Description("The user to generate the image from. (If none given, it uses you.)")] DiscordMember? Member = null) {
            await ctx.TriggerTypingAsync();

            var m = Member ?? ctx.Member;

            using (var client = new WebClient()) {
                using var _ = await client.OpenReadTaskAsync(m.AvatarUrl);
                using var ava = new MagickImage(_);
                using var template = new MagickImage("Resource/Files/memegen/notallowed.png");
                //we use ava twice, so let's clone it just to have two high quality renders to work with.
                using var clone = ava.Clone();
                clone.Resize(340, 340);
                using var bg = new MagickImage(MagickColors.White, 340, 340);
                template.Composite(clone, 485, 83, CompositeOperator.DstOver);
                template.Composite(bg, 485, 83, CompositeOperator.DstOver);
                ava.Resize(390, 390);
                bg.Resize(390, 390);
                template.Composite(bg, 46, 63, CompositeOperator.SrcOver);
                template.Composite(ava, 46, 63, CompositeOperator.SrcOver);
                using var output = new MemoryStream(template.ToByteArray());

                await ctx.RespondWithFileAsync("huh.png", output);
            }
        }

        [Command("technology"), Description("\"We have T E C H N O L O G Y\" ~Patrick"), Category(Category.Meme)]
        async Task Technology(CommandContext ctx,
            [Description("The link to an image to use for this meme. If none given, KekBot will search your command for an attachment. If none found, it'll search message history for an image to use.")]
            Uri? Image = null) {
            await ctx.TriggerTypingAsync();

            Uri? uri = Image;
            //Checks if "Image" was null.
            if (uri == null) {
                if (ctx.Message.Attachments.Count > 0) {
                    uri = new Uri(ctx.Message.Attachments[0].Url);
                } else {
                    uri = await HuntForImage(ctx);
                }
            }
            //Checks if the search failed.
            if (uri == null) {
                await ctx.RespondAsync("No image found.");
                return;
            }

            using (var client = new WebClient()) {
                using var _ = await client.OpenReadTaskAsync(uri);
                using var image = new MagickImage(_);
                using var template = new MagickImage("Resource/Files/memegen/wehavetechnology.png");
                using var bg = new MagickImage(MagickColors.White, 335, 286);


                var widthRatio = 335d / image.Width;
                var heightRatio = 286d / image.Height;
                var ratio = Math.Min(widthRatio, heightRatio);

                var width = (int)(image.Width * ratio);
                var height = (int)(image.Height * ratio);

                var x = (335 / 2) - (width / 2);
                var y = (286 / 2) - (height / 2);

                image.Resize(width, height);
                template.Composite(image, 37 + x, 555 + y, CompositeOperator.DstOver);
                using var output = new MemoryStream(template.ToByteArray());

                await ctx.RespondWithFileAsync("T E C H N O L O G Y.png", output);
            }
        }

        [Command("torture"), Description("The worst torture possible."), Category(Category.Meme)]
        async Task Torture(CommandContext ctx,
            [Description("The link to an image to use for this meme. If none given, KekBot will search your command for an attachment. If none found, it'll search message history for an image to use.")]
            Uri? Image = null) {
            await ctx.TriggerTypingAsync();

            Uri? uri = Image;
            //Checks if "Image" was null.
            if (uri == null) {
                if (ctx.Message.Attachments.Count > 0) {
                    uri = new Uri(ctx.Message.Attachments[0].Url);
                } else {
                    uri = await HuntForImage(ctx);
                }
            }
            //Checks if the search failed.
            if (uri == null) {
                await ctx.RespondAsync("No image found.");
                return;
            }

            using (var client = new WebClient()) {
                using var _ = await client.OpenReadTaskAsync(uri);
                using var image = new MagickImage(_);
                using var template = new MagickImage("Resource/Files/memegen/torture.png");
                using var bg = new MagickImage(MagickColors.Black, 199, 191);


                var widthRatio = 199d / image.Width;
                var heightRatio = 191d / image.Height;
                var ratio = Math.Min(widthRatio, heightRatio);

                var width = (int)(image.Width * ratio);
                var height = (int)(image.Height * ratio);

                var x = (199 / 2) - (width / 2);
                var y = (191 / 2) - (height / 2);

                image.Resize(width, height);
                template.Composite(image, 248 + x, 159 + y, CompositeOperator.DstOver);
                template.Composite(bg, 248, 159, CompositeOperator.DstOver);
                using var output = new MemoryStream(template.ToByteArray());

                await ctx.RespondWithFileAsync("torture.png", output);
            }
        }

        [Command("trashwaifu"), Description("Your waifu is entry level garbage!"), Category(Category.Meme)]
        async Task TrashWaifu(CommandContext ctx,
            [Description("The link to an image to use for this meme. If none given, KekBot will search your command for an attachment. If none found, it'll search message history for an image to use.")]
            Uri? Image = null) {
            await ctx.TriggerTypingAsync();

            Uri? uri = Image;
            //Checks if "Image" was null.
            if (uri == null) {
                if (ctx.Message.Attachments.Count > 0) {
                    uri = new Uri(ctx.Message.Attachments[0].Url);
                } else {
                    uri = await HuntForImage(ctx);
                }
            }
            //Checks if the search failed.
            if (uri == null) {
                await ctx.RespondAsync("No image found.");
                return;
            }

            using (var client = new WebClient()) {
                using var _ = await client.OpenReadTaskAsync(uri);
                using var image = new MagickImage(_);
                using var template = new MagickImage("Resource/Files/memegen/trash_waifu.png");
                using var bg = new MagickImage(MagickColors.White, 139, 167);

                image.Rotate(18.89);
                image.Trim();
                image.Shave(1, 1);

                var widthRatio = 144d / image.Width;
                var heightRatio = 176d / image.Height;
                var ratio = Math.Min(widthRatio, heightRatio);

                var width = (int)(image.Width * ratio);
                var height = (int)(image.Height * ratio);

                var x = (144 / 2) - (width / 2);
                var y = (176 / 2) - (height / 2);

                image.Resize(width, height);
                template.Composite(image, 103 + x, 175 + y, CompositeOperator.DstOver);
                template.Composite(bg, 106, 177, CompositeOperator.DstOver);

                using var output = new MemoryStream(template.ToByteArray());

                await ctx.RespondWithFileAsync("trash.png", output);
            }
        }

        [Command("urgent"), Description("If this is urgent, reply \"urgent\"..."), Category(Category.Meme)]
        async Task Urgent(CommandContext ctx,
            [Description("The link to an image to use for this meme. If none given, KekBot will search your command for an attachment. If none found, it'll search message history for an image to use.")]
            Uri? Image = null) {
            await ctx.TriggerTypingAsync();

            Uri? uri = Image;
            //Checks if "Image" was null.
            if (uri == null) {
                if (ctx.Message.Attachments.Count > 0) {
                    uri = new Uri(ctx.Message.Attachments[0].Url);
                } else {
                    uri = await HuntForImage(ctx);
                }
            }
            //Checks if the search failed.
            if (uri == null) {
                await ctx.RespondAsync("No image found.");
                return;
            }

            using (var client = new WebClient()) {
                using var _ = await client.OpenReadTaskAsync(uri);
                using var image = new MagickImage(_);
                using var template = new MagickImage("Resource/Files/memegen/urgent.png");
                using var bg = new MagickImage(MagickColors.Black, 552, 465);

                var widthRatio = 552d / image.Width;
                var heightRatio = 465d / image.Height;
                var ratio = Math.Min(widthRatio, heightRatio);

                var width = (int)(image.Width * ratio);
                var height = (int)(image.Height * ratio);

                var x = (552 / 2) - (width / 2);
                var y = (465 / 2) - (height / 2);

                image.Resize(width, height);
                template.Composite(image, 22 + x, 11 + y, CompositeOperator.DstOver);
                template.Composite(bg, 22, 11, CompositeOperator.DstOver);

                using var output = new MemoryStream(template.ToByteArray());

                await ctx.RespondWithFileAsync("urgent.png", output);
            }
        }

        [Command("trash"), Description("This piece of trash was mistaken for art?"), Category(Category.Meme)]
        async Task Trash(CommandContext ctx,
            [Description("The link to an image to use for this meme. If none given, KekBot will search your command for an attachment. If none found, it'll search message history for an image to use.")]
            Uri? Image = null) {
            await ctx.TriggerTypingAsync();

            Uri? uri = Image;
            //Checks if "Image" was null.
            if (uri == null) {
                if (ctx.Message.Attachments.Count > 0) {
                    uri = new Uri(ctx.Message.Attachments[0].Url);
                } else {
                    uri = await HuntForImage(ctx);
                }
            }
            //Checks if the search failed.
            if (uri == null) {
                await ctx.RespondAsync("No image found.");
                return;
            }

            using (var client = new WebClient()) {
                using var _ = await client.OpenReadTaskAsync(uri);
                using var image = new MagickImage(_);
                using var clone = image.Clone();
                using var template = new MagickImage("Resource/Files/memegen/trash.png");

                var widthRatio = 382d / image.Width;
                var heightRatio = 262d / image.Height;
                var ratio = Math.Min(widthRatio, heightRatio);

                var width = (int)(image.Width * ratio);
                var height = (int)(image.Height * ratio);

                var x = (382 / 2) - (width / 2);
                var y = (262 / 2) - (height / 2);

                image.Resize(width, height);
                image.BackgroundColor = MagickColors.White;
                image.VirtualPixelMethod = VirtualPixelMethod.Background;
                var settings = new DistortSettings() {
                    Viewport = new MagickGeometry(template.Width, template.Height)
                };
                //Distort the image to match the perspective of the template.
                //Pattern: (Src TL X, Src TL Y, Dest TL X, Dest TL Y, Src TR X, Src TR Y, Dest TR X, Dest TR Y, Src BR X, Src BR Y, Dest BR X, Dest BR Y, Src BL X, Src BL Y, Dest BR X, Dest BR Y)
                image.Distort(DistortMethod.Perspective, settings, 0, 0, 620 + x, 102 + y, image.Width, 0, 983 - x, 200 + y, image.Width, image.Height, 917 - x, 446 - y, 0, image.Height, 551 + x, 334 - y);

                widthRatio = 123d / clone.Width;
                heightRatio = 86d / clone.Height;
                ratio = Math.Min(widthRatio, heightRatio);

                width = (int)(clone.Width * ratio);
                height = (int)(clone.Height * ratio);

                x = (123 / 2) - (width / 2);
                y = (86 / 2) - (height / 2);

                clone.Resize(width, height);
                clone.BackgroundColor = MagickColors.Transparent;
                clone.VirtualPixelMethod = VirtualPixelMethod.Background;
                clone.Distort(DistortMethod.Perspective, settings, 0, 0, 353 + x, 317 + y, clone.Width, 0, 473 - x, 322 + y, clone.Width, clone.Height, 468 - x, 404 - y, 0, clone.Height, 349 + x, 397 - y);
                image.Composite(clone, CompositeOperator.SrcOver);
                template.Composite(image, CompositeOperator.DstOver);
                using var output = new MemoryStream(template.ToByteArray());

                await ctx.RespondWithFileAsync("trash_but_its_not.png", output);
            }
        }

        [Command("byemom"), Description("OK BYE MOM"), Category(Category.Meme)]
        async Task ByeMom(CommandContext ctx, [RemainingText, Description("The text that'll be used in the meme. (Max 50 chars)")] string Text) {
            if (Text.Length > 50) {
                await ctx.RespondAsync("You cannot have more than 50 characters in this command.");
            }
            await ctx.TriggerTypingAsync();

            using (var client = new WebClient()) {
                using var _ = await client.OpenReadTaskAsync(ctx.Member.AvatarUrl);
                using var ava = new MagickImage(_);
                using var clone = ava.Clone();
                using var template = new MagickImage("Resource/Files/memegen/byemom_template.png");
                using var bg = new MagickImage(MagickColors.White, 128, 128);

                var textSettings = new MagickReadSettings() {
                    Font = "Ariel",
                    TextGravity = Gravity.West,
                    BackgroundColor = MagickColors.Transparent,
                    Width = 388,
                    Height = 28
                };
                using var text = new MagickImage($"caption:{Text}", textSettings);
                text.Rotate(-25);

                ava.Resize(80, 80);
                clone.Resize(128, 128);
                template.Composite(bg, 73, 338, CompositeOperator.SrcOver);
                template.Composite(clone, 73, 338, CompositeOperator.SrcOver);
                bg.Resize(80, 80);
                template.Composite(bg, 523, 12, CompositeOperator.SrcOver);
                template.Composite(ava, 523, 12, CompositeOperator.SrcOver);
                template.Composite(text, 345, 426, CompositeOperator.SrcOver);
                using var output = new MemoryStream(template.ToByteArray());

                await ctx.RespondWithFileAsync("byemom.png", output);
            }
        }

        [Command("delet"), Description("Delet a user from existance."), Category(Category.Meme)]
        async Task Delet(CommandContext ctx, [Required, RemainingText, Description("The user to generate the image from. (If none given, it uses you.)")] String Member) {
            await ctx.TriggerTypingAsync();

            if (!String.IsNullOrWhiteSpace(Member) && Member.Equals("this")) {
                // TODO: gonna need hutch to help port the "delet this" easter egg (which used weeb.sh to make that happen) 
                return;
            }

            var target = await Util.ConvertArgAsync<DiscordMember>(Member, ctx);

            var m = target ?? ctx.Member;

            using (var client = new WebClient()) {
                using var _ = await client.OpenReadTaskAsync(m.AvatarUrl);
                using var ava = new MagickImage(_);
                using var template = new MagickImage("Resource/Files/memegen/DELET_template.png");

                ava.Resize(42, 42);
                template.Composite(ava, 36, 114, CompositeOperator.DstOver);

                var textSettings = new MagickReadSettings() {
                    Font = "Whitney-Book",
                    FontPointsize = 16,
                    TextGravity = Gravity.Northwest,
                    FillColor = new MagickColor(m.Color.ToString()), 
                    BackgroundColor = MagickColors.Transparent,
                    Width = 279,
                    Height = 25
                };

                using var text = new MagickImage($"caption:{m.DisplayName}", textSettings);

                template.Composite(text, 93, 113, CompositeOperator.SrcOver);

                //Pull random quote from text file for delet command.
                try {
                    var quotes = await File.ReadAllLinesAsync("Resource/Files/memegen/delet_quotes.txt");
                    var quote = quotes.RandomElement();

                    textSettings.FillColor = MagickColors.White;
                    textSettings.Width = 315;
                    textSettings.Height = 25;
                    textSettings.TextGravity = Gravity.Forget;

                    using var drawnQuote = new MagickImage($"caption:{quote}", textSettings);

                    template.Composite(drawnQuote, 93, 133, CompositeOperator.SrcOver);
                    GC.Collect();
                } catch (Exception e) {
                    ctx.Client.DebugLogger.LogMessage(LogLevel.Error, KekBot.LOGTAG, "Could not generate a quote for the delet command. Field was left blank.", DateTime.Now);
                }

                using var output = new MemoryStream(template.ToByteArray());

                await ctx.RespondWithFileAsync("delet.png", output);
            }
        }

        private async Task<Uri?> HuntForImage(CommandContext ctx) {
            var messages = await ctx.Channel.GetMessagesAsync();
            foreach (var message in messages) {
                //Let's check attachments first.
                if (message.Attachments.Count > 0) {
                    var attach = message.Attachments[0];
                    //Check if actually an image
                    if (attach.IsImage()) {
                        return new Uri(attach.Url);
                    }
                    //No attachments :(
                } else {
                    //Next, we're checking embeds.
                    if (message.Embeds.Count > 0) {
                        var embed = message.Embeds[0];
                        //Embeds can have two image fields. The image, and thumbnail fields. We're checking the image field first.
                        if (embed.Image != null) return embed.Image.Url.ToUri();
                        //There was no image. Maybe there's a thumbnail we can use?
                        if (embed.Thumbnail != null) return embed.Thumbnail.Url.ToUri();
                    }
                    //There weren't any embeds. Last resort. Let's see if there's a URL we can use.
                    var contents = message.Content;
                    Uri? uri = await Util.ConvertArgAsync<Uri>(contents, ctx);
                    if (uri != null) return uri;
                }
            }
            return null;
        }

        /* UNUSED CODE FOR DRAWING ONTO A BLANK CANVAS
         using var canvas = new MagickImage(MagickColors.Transparent, template.Width, template.Height);
         var strokeColor = new DrawableStrokeColor(MagickColors.White);
         var strokeWidth = new DrawableStrokeWidth(1);
         var fillColor = new DrawableFillColor(MagickColors.White);
         var bg = new DrawablePolygon(new PointD(291, 0), new PointD(599, 0), new PointD(599, 253), new PointD(291, 224));
         canvas.Draw(strokeColor, strokeWidth, fillColor, bg);
         canvas.Format = MagickFormat.Png;
         */

    }
}
