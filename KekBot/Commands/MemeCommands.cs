﻿using ImageMagick;
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
using DSharpPlus;
using System.Text;
using System.Runtime.CompilerServices;
using System.Linq;
using DSharpPlus.Lavalink;

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

                image.Resize(892, 1108);
                image.BackgroundColor = MagickColors.Transparent;
                image.Extent(892, 1108, Gravity.Center);
                template.Composite(image, 0, 1112, CompositeOperator.SrcOver);

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

                image.Resize(640, 297);
                image.BackgroundColor = MagickColors.Transparent;
                image.Extent(640, 297, Gravity.Center);
                template.Composite(image, 0, 282, CompositeOperator.DstOver);

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

        [Command("poosy"), Description("\"Poosy...De...stroyer.\" ~Vinesauce Joel"), Category(Category.Meme)]
        async Task Poosy(CommandContext ctx, [HiddenParam, RemainingText] FlagArgs args) {
            await ctx.TriggerTypingAsync();

            var reboot = args.ParseBool("reboot") ?? false;

            await ctx.RespondWithFileAsync($"Resource/Files/memegen/poosy{(reboot ? "-reboot" : "")}.png");
        }

        [Command("justright"), Description("When you need Discord to be just right..."), Category(Category.Meme)]
        async Task JustRight(CommandContext ctx) {
            await ctx.TriggerTypingAsync();
            await ctx.RespondWithFileAsync(Directory.GetFiles("Resource/Files/justright").RandomElement());
        }

        [Command("kirb"), Aliases("poyo"), Description("POYO"), Category(Category.Meme)]
        async Task Kirb(CommandContext ctx) {
            await ctx.TriggerTypingAsync();
            await ctx.RespondWithFileAsync(Directory.GetFiles("Resource/Files/kirb").RandomElement());
        }

        [Command("youtried"), Description("You tried. Here's a gold star!"), Category(Category.Meme)]
        async Task YouTried(CommandContext ctx) {
            await ctx.TriggerTypingAsync();
            await ctx.RespondWithFileAsync(Directory.GetFiles("Resource/Files/youtried").RandomElement());
        }

        [Command("lean"), Description("Leans in your discord."), Category(Category.Meme)]
        async Task Lean(CommandContext ctx) {
            await ctx.TriggerTypingAsync();
            await ctx.RespondWithFileAsync(Directory.GetFiles("Resource/Files/lean").RandomElement());
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

                image.Resize(338, 466);
                image.BackgroundColor = MagickColors.Transparent;
                image.Extent(338, 466, Gravity.Center);
                template.Composite(image, 326, 7, CompositeOperator.DstOver);

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

                image.Resize(309, 225);
                image.BackgroundColor = MagickColors.White;
                image.VirtualPixelMethod = VirtualPixelMethod.Background;
                image.Extent(309, 225, Gravity.Center);
                var settings = new DistortSettings() {
                    Viewport = new MagickGeometry(template.Width, template.Height)
                };
                //Distort the image to match the perspective of the template.
                //Pattern: (Src TL X, Src TL Y, Dest TL X, Dest TL Y, Src TR X, Src TR Y, Dest TR X, Dest TR Y, Src BR X, Src BR Y, Dest BR X, Dest BR Y, Src BL X, Src BL Y, Dest BR X, Dest BR Y)
                image.Distort(DistortMethod.Perspective, settings, 0, 0, 291, 0, image.Width, 0, 599, 0, image.Width, image.Height, 599, 253, 0, image.Height, 291, 224);
                template.Composite(image, CompositeOperator.DstOver);
                using var output = new MemoryStream(template.ToByteArray());

                await ctx.RespondWithFileAsync("poster.png", output);
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

                image.Resize(335, 286);
                image.BackgroundColor = MagickColors.White;
                image.Extent(335, 286, Gravity.Center);
                template.Composite(image, 37, 555, CompositeOperator.DstOver);
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

                var widthRatio = 199d / image.Width;
                var heightRatio = 191d / image.Height;
                var ratio = Math.Min(widthRatio, heightRatio);

                var width = (int)(image.Width * ratio);
                var height = (int)(image.Height * ratio);

                var x = (199 / 2) - (width / 2);
                var y = (191 / 2) - (height / 2);

                image.Resize(199, 191);
                image.BackgroundColor = MagickColors.Black;
                image.Extent(199, 191, Gravity.Center);
                template.Composite(image, 248, 159, CompositeOperator.DstOver);
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

                image.Resize(144, 176);
                image.BackgroundColor = MagickColors.Transparent;
                image.Extent(144, 176, Gravity.Center);
                template.Composite(image, 103, 175, CompositeOperator.DstOver);
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

                image.Resize(552, 465);
                image.BackgroundColor = MagickColors.Transparent;
                image.Extent(552, 465, Gravity.Center);
                template.Composite(image, 22, 11, CompositeOperator.DstOver);
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

                image.Resize(382, 262);
                image.BackgroundColor = MagickColors.White;
                image.VirtualPixelMethod = VirtualPixelMethod.Background;
                image.Extent(382, 262, Gravity.Center);
                var settings = new DistortSettings() {
                    Viewport = new MagickGeometry(template.Width, template.Height)
                };
                //Distort the image to match the perspective of the template.
                //Pattern: (Src TL X, Src TL Y, Dest TL X, Dest TL Y, Src TR X, Src TR Y, Dest TR X, Dest TR Y, Src BR X, Src BR Y, Dest BR X, Dest BR Y, Src BL X, Src BL Y, Dest BR X, Dest BR Y)
                image.Distort(DistortMethod.Perspective, settings, 0, 0, 620, 102, image.Width, 0, 983, 200, image.Width, image.Height, 917, 446, 0, image.Height, 551, 334);

                clone.Resize(123, 86);
                clone.BackgroundColor = MagickColors.Transparent;
                clone.VirtualPixelMethod = VirtualPixelMethod.Background;
                clone.Extent(123, 86, Gravity.Center);
                clone.Distort(DistortMethod.Perspective, settings, 0, 0, 353, 317, clone.Width, 0, 473, 322, clone.Width, clone.Height, 468, 404, 0, clone.Height, 349, 397);
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
                using var text = new MagickImage($"caption:{Text.Replace("\\", "\\\\")}", textSettings);
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

            //If we don't include `?? ""`, an error will be thrown in console if no input is given.
            var target = await Util.ConvertArgAsync<DiscordMember>(Member ?? "", ctx);

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

        [Command("doggo"), Description("Doggo displays your text/image on his board!"), Category(Category.Meme), Priority(1)]
        async Task Doggo(CommandContext ctx, [Description("The link to an image to use for this meme. If none given, KekBot will search your command for an attachment. If none found, it'll search message history for an image to use.")]
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
                using var template = new MagickImage("Resource/Files/memegen/doggo.jpg");

                image.Resize(376, 299);
                image.BackgroundColor = MagickColors.Transparent;
                image.Extent(376, 299, Gravity.Center);
                template.Composite(image, 135, 57, CompositeOperator.SrcOver);
                using var output = new MemoryStream(template.ToByteArray());

                await ctx.RespondWithFileAsync("doggo.png", output);
            }
        }

        [Command("doggo"), Priority(0)]
        async Task Doggo(CommandContext ctx, [Description("The text that'll be used in the meme."), RemainingText] string Text) {
            await ctx.TriggerTypingAsync();

            using var template = new MagickImage("Resource/Files/memegen/doggo.jpg");

            var textSettings = new MagickReadSettings() {
                Font = "Calibri-Bold",
                TextGravity = Gravity.Center,
                FillColor = MagickColors.Black,
                BackgroundColor = MagickColors.Transparent,
                Width = 376,
                Height = 299
            };

            using var text = new MagickImage($"caption:{PrepText(Text).Replace("\\", "\\\\")}", textSettings);

            template.Composite(text, 135, 57, CompositeOperator.SrcOver);
            using var output = new MemoryStream(template.ToByteArray());

            await ctx.RespondWithFileAsync("doggo.png", output);
        }

        [Command("kaede"), Description("Kaede holds up a sign, whatever the sign contains is up to you."), Category(Category.Meme), Priority(1)]
        async Task Kaede(CommandContext ctx, [Description("The link to an image to use for this meme. If none given, KekBot will search your command for an attachment. If none found, it'll search message history for an image to use.")]
            Uri? Image = null, [HiddenParam, RemainingText] FlagArgs flags = new FlagArgs()) {
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

            var reboot = flags.ParseBool("reboot") ?? false;

            using (var client = new WebClient()) {
                using var _ = await client.OpenReadTaskAsync(uri);
                using var image = new MagickImage(_);
                using var template = new MagickImage($"Resource/Files/memegen/kaededab{(reboot ? "-reboot" : "")}.jpg");

                image.Resize(610, 379);
                image.BackgroundColor = MagickColors.Transparent;
                image.Extent(610, 379, Gravity.Center);
                image.Rotate(-6.92);
                template.Composite(image, 144, 628, CompositeOperator.SrcOver);
                using var output = new MemoryStream(template.ToByteArray());

                await ctx.RespondWithFileAsync("kaededab.png", output);
            }
        }

        [Command("kaede"), Priority(0)]
        async Task Kaede(CommandContext ctx, [Description("The text that'll be used in the meme."), RemainingText] string Text) {
            await ctx.TriggerTypingAsync();

            var flags = FlagArgs.ParseString(Text, out var stripped) ?? new FlagArgs();

            var reboot = flags.ParseBool("reboot") ?? false;


            using var template = new MagickImage($"Resource/Files/memegen/kaededab{(reboot ? "-reboot" : "")}.jpg");

            var textSettings = new MagickReadSettings() {
                Font = "Calibri-Bold",
                TextGravity = Gravity.Center,
                FillColor = MagickColors.Black,
                BackgroundColor = MagickColors.Transparent,
                Width = 610,
                Height = 379
            };

            using var text = new MagickImage($"caption:{PrepText(stripped).Replace("\\", "\\\\")}", textSettings);
            text.Rotate(-6.92);
            template.Composite(text, 144, 628, CompositeOperator.SrcOver);
            using var output = new MemoryStream(template.ToByteArray());

            await ctx.RespondWithFileAsync("kaededab.png", output);

        }

        [Command("switch"), Description("Shows how easy it is to setup a switch, with a twist."), Category(Category.Meme)]
        async Task Switch(CommandContext ctx, [Description("The link to an image to use for this meme. If none given, KekBot will search your command for an attachment. If none found, it'll search message history for an image to use.")] Uri? Image = null) {
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
                using var template = new MagickImage("Resource/Files/memegen/switch_setup.png");

                image.Resize(174, 157);
                image.BackgroundColor = MagickColors.Transparent;
                image.Extent(174, 157, Gravity.Center);
                template.Composite(image, 366, 214, CompositeOperator.DstOver);

                using var output = new MemoryStream(template.ToByteArray());

                await ctx.RespondWithFileAsync("swatch.png", output);
            }
        }

        [Command("www"), Description("Thanks to the miracle of the world wide web, I can search anything I want!"), Category(Category.Meme)]
        async Task WWW(CommandContext ctx, [Description("The link to an image to use for this meme. If none given, KekBot will search your command for an attachment. If none found, it'll search message history for an image to use.")] Uri? Image = null) {
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
                using var template = new MagickImage("Resource/Files/memegen/www.png");

                image.Resize(165, 131);
                image.BackgroundColor = MagickColors.Transparent;
                image.Extent(165, 131, Gravity.Center);
                template.Composite(image, 132, 466, CompositeOperator.DstOver);

                using var output = new MemoryStream(template.ToByteArray());

                await ctx.RespondWithFileAsync("marvelous.png", output);
            }
        }

        //This command was the most bullshit to implement to CLOSELY imitate the java version. Never. Again.
        [Command("gru"), Description("Gru demonstrates your master plan...?"), Category(Category.Meme), ExtendedDescription("I wonder what would happen if you typed `--hyper` at the end..."), Priority(2)]
        async Task Gru(CommandContext ctx, [Description("Text surrounded in \"quotes\", or a URL to an image.")] string Input1, [Description("If no input given, Input1 will carry over.")] string Input2, [Description("If no input given, Input2 will carry over.")] string Input3, [HiddenParam, RemainingText] FlagArgs f = new FlagArgs()) {
            await ctx.TriggerTypingAsync();

            //This is SUPER gross but it lets us do things the way we want, shush.
            var flags1 = FlagArgs.ParseString(Input1, out var stripped1) ?? new FlagArgs();
            var flags2 = FlagArgs.ParseString(Input2, out var stripped2) ?? new FlagArgs();
            var flags3 = FlagArgs.ParseString(Input3, out var stripped3) ?? new FlagArgs();

            var egg = f.ParseBool("egg") ?? flags1.ParseBool("egg") ?? flags2.ParseBool("egg") ?? flags3.ParseBool("egg") ?? false;
            var hyper = f.ParseBool("hyper") ?? flags1.ParseBool("hyper") ?? flags2.ParseBool("hyper") ?? flags3.ParseBool("hyper") ?? false;

            //WELCOME TO MY BULLSHIT DETECTOR
            if (string.IsNullOrWhiteSpace(stripped1)) {
                stripped1 = string.IsNullOrWhiteSpace(stripped2) ? string.IsNullOrWhiteSpace(stripped3) ? null : stripped3 : stripped2;
            }

            if (string.IsNullOrWhiteSpace(stripped2)) {
                stripped2 = string.IsNullOrWhiteSpace(stripped1) ? string.IsNullOrWhiteSpace(stripped3) ? null : stripped3 : stripped1;
            }

            if (string.IsNullOrWhiteSpace(stripped3)) {
                stripped3 = string.IsNullOrWhiteSpace(stripped1) ? string.IsNullOrWhiteSpace(stripped2) ? null : stripped2 : stripped1;
            }

            if (stripped1 == null) {
                await ctx.RespondAsync("No valid arguments.");
                return;
            }

            using var result = await GenerateGru(ctx, stripped1, stripped2, stripped3, egg, hyper);
            await ctx.RespondWithFileAsync("masterplan.png", result);
        }

        [Command("gru"), Priority(1)]
        async Task Gru(CommandContext ctx, [Description("Text surrounded in \"quotes\", or a URL to an image.")] string Input1, [Description("If no input given, Input1 will carry over.")] string Input2) {
            await ctx.TriggerTypingAsync();

            //This is gross but it lets us do things the way we want, shush.
            var flags1 = FlagArgs.ParseString(Input1, out var stripped1) ?? new FlagArgs();
            var flags2 = FlagArgs.ParseString(Input2, out var stripped2) ?? new FlagArgs();

            var egg = flags1.ParseBool("egg") ?? flags2.ParseBool("egg") ?? false;
            var hyper = flags1.ParseBool("hyper") ?? flags2.ParseBool("hyper") ?? false;

            //WELCOME TO MY BULLSHIT DETECTOR (two argument edition)
            if (string.IsNullOrWhiteSpace(stripped1)) {
                stripped1 = string.IsNullOrWhiteSpace(stripped2) ? null : stripped2;
            }

            if (string.IsNullOrWhiteSpace(stripped2)) {
                stripped2 = string.IsNullOrWhiteSpace(stripped1) ? null : stripped1;
            }

            if (stripped1 == null) {
                await ctx.RespondAsync("No valid arguments.");
                return;
            }

            using var result = await GenerateGru(ctx, stripped1, stripped2, stripped2, egg, hyper);
            await ctx.RespondWithFileAsync("masterplan.png", result);
        }

        [Command("gru"), Priority(0)]
        async Task Gru(CommandContext ctx, [Description("Text surrounded in \"quotes\", or a URL to an image.")] string Input) {
            await ctx.TriggerTypingAsync();

            //This is gross but it lets us do things the way we want, shush.
            var flags1 = FlagArgs.ParseString(Input, out var stripped) ?? new FlagArgs();

            var egg = flags1.ParseBool("egg") ?? false;
            var hyper = flags1.ParseBool("hyper") ?? false;

            //WELCOME TO MY BULLSHIT DETECTOR (single argument edition)
            if (string.IsNullOrWhiteSpace(stripped)) {
                await ctx.RespondAsync("No valid arguments.");
                return;
            }

            using var result = await GenerateGru(ctx, stripped, stripped, stripped, egg, hyper);
            await ctx.RespondWithFileAsync("masterplan.png", result);
        }

        async Task<MemoryStream> GenerateGru(CommandContext ctx, string i1, string i2, string i3, bool egg, bool hyper) {
            //Converts our first input to a URI, will be null if not a valid URL.
            Uri uri1 = await Util.ConvertArgAsync<Uri>(i1, ctx);
            //If our second input is the same as our first, we reuse the first variable, otherwise convert to URI.
            Uri uri2 = i2 == i1 ? uri1 : await Util.ConvertArgAsync<Uri>(i2, ctx);
            //Same logic applies here.
            Uri uri3 = i3 == i2 ? uri2 : await Util.ConvertArgAsync<Uri>(i3, ctx);

            using var template = new MagickImage($"Resource/Files/memegen/grusmasterplan{(egg ? "egg" : "")}{(hyper ? "hyper" : "")}.png");

            if (uri1 != null) await DrawImage(uri1, template, egg ? 493 : 436, 114);
            else DrawText(i1, template, egg ? 493 : 436, 114);
            if (uri2 != null) await DrawImage(uri2, template, egg ? 1343 : 1191, 114);
            else DrawText(i2, template, egg ? 1343 : 1191, 114);
            if (uri3 != null) {
                await DrawImage(uri3, template, egg ? 491 : 442, 595);
                await DrawImage(uri3, template, egg ? 1347 : 1190, 595);
                if (hyper) await DrawImage(uri3, template, egg ? 918 : 786, 1073);
            } else {
                DrawText(i3, template, egg ? 491 : 442, 595);
                DrawText(i3, template, egg ? 1347 : 1190, 595);
                if (hyper) DrawText(i3, template, egg ? 918 : 786, 1073);
            }

            return new MemoryStream(template.ToByteArray());

            //A method in a method? Crazy.
            async Task DrawImage(Uri uri, MagickImage template, int x, int y) {
                using (var client = new WebClient()) {
                    using var img = await client.OpenReadTaskAsync(uri);
                    using var image = new MagickImage(img) {
                        BackgroundColor = MagickColors.Transparent
                    };
                    image.Resize(270, 360);
                    image.Extent(270, 360, Gravity.Center);

                    template.Composite(image, x, y, CompositeOperator.SrcOver);
                }
            }

            //Another method in a method? Crazy.
            void DrawText(string str, MagickImage template, int x, int y) {
                var textSettings = new MagickReadSettings() {
                    Font = "Calibri-Bold",
                    TextGravity = Gravity.Center,
                    FillColor = MagickColors.Black,
                    BackgroundColor = MagickColors.Transparent,
                    Width = 270,
                    Height = 360
                };

                using var text = new MagickImage($"caption:{PrepText(str).Replace("\\", "\\\\")}", textSettings);

                template.Composite(text, x, y, CompositeOperator.SrcOver);
            }
        }

        sealed class MemeVoiceCommands : BaseCommandModule {
            private MusicService Music { get; }
            private GuildMusicData GuildMusic { get; set; }

            public MemeVoiceCommands(MusicService music) {
                this.Music = music;
            }

            public override async Task BeforeExecutionAsync(CommandContext ctx) {
                this.GuildMusic = await Music.GetDataAsync(ctx.Guild);
                if (this.GuildMusic != null && GuildMusic.IsPlaying && !this.GuildMusic.IsMeme) {
                    throw new CommandCancelledException();
                }
                var vs = ctx.Member.VoiceState;
                var chn = vs?.Channel;
                if (chn == null) {
                    await ctx.RespondAsync($"You need to be in a voice channel. (Debug Message)");
                    throw new CommandCancelledException();
                }

                var mbr = ctx.Guild.CurrentMember?.VoiceState?.Channel;
                if (mbr != null && chn != mbr) {
                    await ctx.RespondAsync($"You need to be in the same voice channel. (Debug Message)");
                    throw new CommandCancelledException();
                }

                this.GuildMusic = await this.Music.GetOrCreateDataAsync(ctx.Guild);

                await base.BeforeExecutionAsync(ctx);
            }

            //A few hacky workarounds to get the music player to play memes, but it's better than nothing.
            [Command("granddad"), Description("FLEENSTONES!?"), Category(Category.Meme)]
            async Task GrandDad(CommandContext ctx, [HiddenParam] FlagArgs flags = new FlagArgs()) {
                var reboot = flags.ParseBool("reboot") ?? false;
                var trackLoad = await Music.GetTracksFromFileAsync(new FileInfo(Directory.GetFiles($"Resource/Files/sound/granddad{(reboot ? "/reboot" : "")}").RandomElement()));
                var tracks = trackLoad.Tracks;
                foreach (var track in tracks)
                    this.GuildMusic.Enqueue(new MusicItem(track, ctx.Member));

                var vs = ctx.Member.VoiceState;
                var chn = vs.Channel;
                await this.GuildMusic.CreateMemeAsync(chn);
                await this.GuildMusic.PlayAsync();
            }

            [Command("jontron"), Description("Ech ~Jontron"), Category(Category.Meme)]
            async Task Jontron(CommandContext ctx, [HiddenParam] FlagArgs flags = new FlagArgs()) {
                var reboot = flags.ParseBool("reboot") ?? false;
                var trackLoad = await Music.GetTracksFromFileAsync(new FileInfo(Directory.GetFiles($"Resource/Files/sound/jontron{(reboot ? "/reboot" : "")}").RandomElement()));
                var tracks = trackLoad.Tracks;
                foreach (var track in tracks)
                    this.GuildMusic.Enqueue(new MusicItem(track, ctx.Member));

                var vs = ctx.Member.VoiceState;
                var chn = vs.Channel;
                await this.GuildMusic.CreateMemeAsync(chn);
                await this.GuildMusic.PlayAsync();
            }

            [Command("gabe"), Description("\"Bork!\" ~Gabe 2k17 <3"), Aliases("bork"), Category(Category.Meme)]
            async Task Gabe(CommandContext ctx, [HiddenParam] FlagArgs flags = new FlagArgs()) {
                var reboot = flags.ParseBool("reboot") ?? false;
                var trackLoad = await Music.GetTracksFromFileAsync(new FileInfo(Directory.GetFiles($"Resource/Files/sound/gabe{(reboot ? "/reboot" : "")}").RandomElement()));
                var tracks = trackLoad.Tracks;
                foreach (var track in tracks)
                    this.GuildMusic.Enqueue(new MusicItem(track, ctx.Member));

                var vs = ctx.Member.VoiceState;
                var chn = vs.Channel;
                await this.GuildMusic.CreateMemeAsync(chn);
                await this.GuildMusic.PlayAsync();
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

        private string PrepText(string text) {
            var charLimit = 12;
            var sb = new StringBuilder();
            var split = text.Split(" ");
            for (var i = 0; i < split.Length; i++) {
                if (split[i].Length > charLimit) {
                    for (var ii = 0; ii < split[i].Length; ii += charLimit) sb.Append(split[i].Substring(ii, Math.Min(charLimit, split[i].Length - ii)) + " ");
                } else sb.Append(split[i] + " ");
            }
            return sb.ToString();
        }
    }
}
