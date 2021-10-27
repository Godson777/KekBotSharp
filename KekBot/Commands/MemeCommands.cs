using ImageMagick;
using System;
using System.Collections.Generic;
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
using System.Net.Mime;
using DSharpPlus.Lavalink;
using DSharpPlus.SlashCommands;
using KekBot.Lib;
using Microsoft.Extensions.Logging;

namespace KekBot.Commands {
    public enum AvatarPreference
    {
        [ChoiceName("prefer server avatar")]
        Guild = 0, // default
        [ChoiceName("use global avatar")]
        Global,
    }
    
    class MemeCommands : ApplicationCommandModule
    {

        private const string UserArgOrYouDescription =
            "The user to generate the image from. (If none given, it uses you.)";

        private const string UserArgRequiredDescription = "The user to target for the image.";

        private const string AvatarArgName = "avatar-preference";
        private const string AvatarArgDescription =
            "Whether to use their avatar on this server (the default), or their global one.";

        private const string ImageArgDescription =
            "An image link. If none given, KekBot will search message history for an image to use.";
        
        private const string RebootArgName = "show-reboot";
        private const string RebootArgDescription =
            "Show the alternate image from one of the \"reboot\" events.";

        private const string AvatarFailed = "Error while loading avatar.";
        private const string ImageNotFound = "No image found.";

        private readonly Randumb Random = Randumb.Instance;
        
        private static async Task<Uri?> HuntForImage(InteractionContext ctx)
        {
            foreach (var message in await ctx.Channel.GetMessagesAsync())
            {
                //Let's check attachments first.
                if (message.Attachments.Count > 0)
                {
                    var attach = message.Attachments[0];
                    //Check if actually an image
                    if (attach.IsImage()) return new Uri(attach.Url);
                    //No attachments :(
                }
                else
                {
                    //Next, we're checking embeds.
                    if (message.Embeds.Count > 0)
                    {
                        var embed = message.Embeds[0];
                        //Embeds can have two image fields. The image, and thumbnail fields. We're checking the image field first.
                        if (embed.Image != null) return embed.Image.Url.ToUri();
                        //There was no image. Maybe there's a thumbnail we can use?
                        if (embed.Thumbnail != null) return embed.Thumbnail.Url.ToUri();
                    }
                    
                    //There weren't any embeds. Last resort. Let's see if there's a URL we can use.
                    if (message.Content == null) continue;
                    
                    try
                    {
                        return new Uri(message.Content);
                    }
                    catch (FormatException)
                    { }
                }
            }
            
            return null;
        }

        static async Task<MagickImage?> GetImageArg(InteractionContext ctx, string? uriString)
        {
            Uri? uri;
            
            // Check if an image URI was given
            if (uriString != null)
                uri = new Uri(uriString);
            // Slash commands don't support attachments yet
            // else if (ctx.Attachments.Count > 0)
            //     uri = new Uri(ctx.Interaction.Attachments[0].Url);
            // Search previous messages for images
            else
                uri = await HuntForImage(ctx);

            if (uri == null) return null;
            
            using var client = new WebClient();
            await using var imgStream = await client.OpenReadTaskAsync(uri);
            return new MagickImage(imgStream);
        }
        
        // todo: figure out what this does
        private static string PrepText(string text) {
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
        
        [SlashCommand("brave", "I'm a brave boy!"), Category(Category.Meme)]
        async Task Brave(InteractionContext ctx,
            [Option("image", ImageArgDescription)] string? uriString = null)
        {
            await ctx.SendThinking();

            using var image = await GetImageArg(ctx, uriString);
            // Checks if the search failed.
            if (image == null) {
                await ctx.EditBasicAsync(ImageNotFound);
                return;
            }
            
            using var template = new MagickImage("Resource/Files/memegen/brave.jpg");

            image.Resize(892, 1108);
            image.BackgroundColor = MagickColors.Transparent;
            image.Extent(892, 1108, Gravity.Center);
            template.Composite(image, 0, 1112, CompositeOperator.SrcOver);

            await using var output = new MemoryStream(template.ToByteArray());
            await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddFile("brave.png", output));
        }
        
        [SlashCommand("byemom", "OK BYE MOM"), Category(Category.Meme)]
        async Task ByeMom(InteractionContext ctx,
            [Option("text", "The text that'll be used in the meme. (Max 50 chars)")] string text,
            [Option(AvatarArgName, AvatarArgDescription)] AvatarPreference avaPref = default)
        {
            if (text.Length > 50) {
                await ctx.ReplyBasicAsync("You cannot have more than 50 characters in this command.");
                return;
            }

            await ctx.SendThinking();

            await using var avaStream = await ctx.Member.OpenReadAvatarAsync(avaPref);
            if (avaStream == null)
            {
                await ctx.EditBasicAsync(AvatarFailed);
                return;
            }
            
            using var ava = new MagickImage(avaStream);
            using var clone = ava.Clone();
            using var template = new MagickImage("Resource/Files/memegen/byemom_template.png");
            using var bg = new MagickImage(MagickColors.White, 128, 128);

            var textSettings = new MagickReadSettings
            {
                Font = "Ariel",
                TextGravity = Gravity.West,
                BackgroundColor = MagickColors.Transparent,
                Width = 388,
                Height = 28
            };
            using var textImg = new MagickImage($"caption:{text.Replace("\\", "\\\\")}", textSettings);
            textImg.Rotate(-25);

            ava.Resize(80, 80);
            clone.Resize(128, 128);
            template.Composite(bg, 73, 338, CompositeOperator.SrcOver);
            template.Composite(clone, 73, 338, CompositeOperator.SrcOver);
            bg.Resize(80, 80);
            template.Composite(bg, 523, 12, CompositeOperator.SrcOver);
            template.Composite(ava, 523, 12, CompositeOperator.SrcOver);
            template.Composite(textImg, 345, 426, CompositeOperator.SrcOver);
            await using var output = new MemoryStream(template.ToByteArray());

            await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddFile("byemom.png", output));
        }

        [SlashCommandGroup("doggo", "Doggo displays your text/image on his board!"), Category(Category.Meme)]
        class DoggoCommands : ApplicationCommandModule
        {
            [SlashCommand("image", "Doggo displays your image on his board!")]
            async Task DoggoImage(InteractionContext ctx,
                [Option("image", ImageArgDescription)] string? uriString = null)
            {
                await ctx.SendThinking();

                using var image = await GetImageArg(ctx, uriString);
                if (image == null)
                {
                    await ctx.EditBasicAsync(ImageNotFound);
                    return;
                }

                image.Resize(376, 299);
                image.BackgroundColor = MagickColors.Transparent;
                image.Extent(376, 299, Gravity.Center);

                await Base(ctx, image);
            }
            
            [SlashCommand("text", "Doggo displays your text on his board!")]
            async Task DoggoText(InteractionContext ctx,
                [Option("text", "The text that'll be used in the meme.")] string text)
            {
                await ctx.SendThinking();

                var textSettings = new MagickReadSettings
                {
                    Font = "Calibri-Bold",
                    TextGravity = Gravity.Center,
                    FillColor = MagickColors.Black,
                    BackgroundColor = MagickColors.Transparent,
                    Width = 376,
                    Height = 299
                };

                using var image =
                    new MagickImage($"caption:{PrepText(text).Replace("\\", "\\\\")}",
                        textSettings);

                await Base(ctx, image);
            }

            /// <summary>
            /// You <em>must</em> send a deferred response before calling this.
            /// </summary>
            /// <param name="ctx">Slash command context</param>
            /// <param name="image">Image to put on the template</param>
            private async Task Base(InteractionContext ctx, MagickImage image)
            {
                using var template = new MagickImage("Resource/Files/memegen/doggo.jpg");
                template.Composite(image, 135, 57, CompositeOperator.SrcOver);
                await using var output = new MemoryStream(template.ToByteArray());

                await ctx.EditResponseAsync(
                    new DiscordWebhookBuilder().AddFile("doggo.png", output));
            }
        }

        [SlashCommandGroup("delet", "Delet a user from existence."), Category(Category.Meme)]
        class DeletCommands : ApplicationCommandModule
        {
            public WeebCommandsBase WeebBase { private get; set; }
            
            [SlashCommand("this", "What can I say except delet this?")]
            async Task DeletThis(InteractionContext ctx)
            {
                await WeebBase.FetchAndPost(ctx, "delet_this", "", "");
            }
            
            [SlashCommand("user", "Delet a user from existence.")]
            async Task DeletUser(InteractionContext ctx,
                [Option("user", UserArgOrYouDescription)] DiscordUser? user = null,
                [Option(AvatarArgName, AvatarArgDescription)] AvatarPreference avaPref = default)
            {
                await ctx.SendThinking();

                var m = (DiscordMember)(user ?? ctx.Member);

                using var client = new WebClient();
                await using var avaStream = await m.OpenReadAvatarAsync(avaPref);
                if (avaStream == null)
                {
                    await ctx.EditBasicAsync(AvatarFailed);
                    return;
                }
                using var ava = new MagickImage(avaStream);
                using var template = new MagickImage("Resource/Files/memegen/DELET_template.png");

                ava.Resize(42, 42);
                template.Composite(ava, 36, 114, CompositeOperator.DstOver);

                var textSettings = new MagickReadSettings
                {
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
                try
                {
                    var quotes = await File.ReadAllLinesAsync("Resource/Files/memegen/delet_quotes.txt");
                    var quote = quotes.RandomElement();

                    textSettings.FillColor = MagickColors.White;
                    textSettings.Width = 315;
                    textSettings.Height = 25;
                    textSettings.TextGravity = Gravity.Forget;

                    using var drawnQuote = new MagickImage($"caption:{quote}", textSettings);

                    template.Composite(drawnQuote, 93, 133, CompositeOperator.SrcOver);
                    GC.Collect();
                }
                catch (Exception)
                {
                    ctx.Client.Logger.Log(LogLevel.Error, KekBot.LOGTAG, "Could not generate a quote for the delet command. Field was left blank.", DateTime.Now);
                }

                await using var output = new MemoryStream(template.ToByteArray());

                await ctx.EditResponseAsync(
                    new DiscordWebhookBuilder().AddFile("delet.png", output));
            }
        }

        [SlashCommand("doorkick", "WHAT DID I JUST WALK INTO"), Category(Category.Meme)]
        async Task DoorKick(InteractionContext ctx,
            [Option("image", ImageArgDescription)] string? uriString = null)
        {
            await ctx.SendThinking();

            using var image = await GetImageArg(ctx, uriString);
            // Checks if the search failed.
            if (image == null) {
                await ctx.EditBasicAsync(ImageNotFound);
                return;
            }

            using var template = new MagickImage("Resource/Files/memegen/door.png");

            image.Resize(338, 466);
            image.BackgroundColor = MagickColors.Transparent;
            image.Extent(338, 466, Gravity.Center);
            template.Composite(image, 326, 7, CompositeOperator.DstOver);

            await using var output = new MemoryStream(template.ToByteArray());

            await ctx.EditResponseAsync(
                new DiscordWebhookBuilder().AddFile("NOPE_NVM.png", output));
        }
        
        [SlashCommand("doubt", "Displays your doubt."), Category(Category.Meme)]
        async Task Doubt(InteractionContext ctx,
            [Option(AvatarArgName, AvatarArgDescription)] AvatarPreference avaPref = default)
        {
            await ctx.SendThinking();

            using var client = new WebClient();
            await using var avaStream = await ctx.Member.OpenReadAvatarAsync(avaPref);
            if (avaStream == null)
            {
                await ctx.EditBasicAsync(AvatarFailed);
                return;
            }
            using var ava = new MagickImage(avaStream);
            using var template = new MagickImage("Resource/Files/memegen/doubt.png");
            ava.Resize(709, 709);
            template.Composite(ava, 0, 0, CompositeOperator.SrcOver);

            await using var output = new MemoryStream(template.ToByteArray());

            await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddFile("doubt.png", output));
        }
        
        [SlashCommand("erase", "For really big mistakes."), Category(Category.Meme)]
        async Task Erase(InteractionContext ctx,
            [Option("user", UserArgOrYouDescription)] DiscordUser? user = null,
            [Option(AvatarArgName, AvatarArgDescription)] AvatarPreference avaPref = default)
        {
            await ctx.SendThinking();

            var m = (DiscordMember)(user ?? ctx.Member);

            using var client = new WebClient();
            await using var avaStream = await m.OpenReadAvatarAsync(avaPref);
            if (avaStream == null)
            {
                await ctx.EditBasicAsync(AvatarFailed);
                return;
            }
            using var ava = new MagickImage(avaStream);
            using var template = new MagickImage("Resource/Files/memegen/mistake_template.png");
            ava.Resize(270, 270);
            template.Composite(ava, 368, 375, CompositeOperator.SrcOver);

            await using var output = new MemoryStream(template.ToByteArray());

            await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddFile("erase.png", output));
        }
        
        [SlashCommand("garage", "Show your friends what's in your garage!"), Category(Category.Meme)]
        async Task Garage(InteractionContext ctx,
            [Option("image", ImageArgDescription)] string? uriString = null)
        {
            await ctx.SendThinking();

            using var image = await GetImageArg(ctx, uriString);
            // Checks if the search failed.
            if (image == null) {
                await ctx.EditBasicAsync(ImageNotFound);
                return;
            }
            
            using var template = new MagickImage("Resource/Files/memegen/garage.png");

            image.Resize(640, 297);
            image.BackgroundColor = MagickColors.Transparent;
            image.Extent(640, 297, Gravity.Center);
            template.Composite(image, 0, 282, CompositeOperator.DstOver);

            await using var output = new MemoryStream(template.ToByteArray());

            await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddFile("garage.png", output));
        }
        
        [SlashCommand("gbj", "Sends the target of your choosing to gay baby jail."), Category(Category.Meme)]
        async Task GBJ(InteractionContext ctx,
            [Option("user", UserArgRequiredDescription)] DiscordUser user,
            [Option(AvatarArgName, AvatarArgDescription)] AvatarPreference avaPref = default)
        {
            await ctx.SendThinking();

            await using var avaStream = await ((DiscordMember)user).OpenReadAvatarAsync(avaPref);
            if (avaStream == null)
            {
                await ctx.EditBasicAsync(AvatarFailed);
                return;
            }
            
            using var target = new MagickImage(avaStream) {
                ColorSpace = ColorSpace.Gray
            };
            target.Resize(430, 430);
            using var gbj = new MagickImage("Resource/Files/memegen/gbj.png");

            gbj.Composite(target, 109, 169, CompositeOperator.DstOver);

            await using var output = new MemoryStream(gbj.ToByteArray());

            await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddFile("gbj.png", output));
        }

        [SlashCommand("gril", "Shows a topless gril."), Category(Category.Meme)]
        async Task Gril(InteractionContext ctx,
            [Option(RebootArgName, RebootArgDescription)] bool reboot = false)
        {
            await ctx.SendThinking();
            await using var file =
                new FileStream(
                    $"Resource/Files/memegen/topless_grill{(reboot ? "-reboot" : "")}.png",
                    FileMode.Open);
            await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddFile(file));
        }
        
        enum GruVariant
        {
            [ChoiceName("original")]
            Gru = 0, // default
            [ChoiceName("egg")]
            Eggman,
        }
        
        [SlashCommand("gru", "Gru demonstrates your master plan...?"), Category(Category.Meme)]
        async Task Gru(InteractionContext ctx,
            [Option("input1", "Text or a URL to an image.")] string input1,
            [Option("input2", "If not given, input1 will carry over.")] string? input2 = null,
            [Option("input3", "If not given, input2 will carry over.")] string? input3 = null,
            [Option("variant", "Gru, is that you...?")] GruVariant variant = default,
            [Option("hyper", "I wonder what this does...")] bool hyper = false)
        {
            await ctx.SendThinking();

            static async Task<MagickImage?> TryFetchImage(string? uriStr)
            {
                if (!Util.TryParseUri(uriStr, out var uri)) return null;

                using var client = new WebClient();
                await using var stream = await client.OpenReadTaskAsync(uri);
#pragma warning disable CA2000
                // I can't dispose of this yet, I use it later!
                var image = new MagickImage(stream)
                {
                    BackgroundColor = MagickColors.Transparent
                };
#pragma warning restore CA2000
                image.Resize(270, 360);
                image.Extent(270, 360, Gravity.Center);
                return image;
            }

            MagickImage? image1, image2, image3;
            try
            {
                (
                    image1,
                    image2,
                    image3
                ) = await Util.AllTasks(
                    TryFetchImage(input1),
                    TryFetchImage(input2),
                    TryFetchImage(input3)
                );
            }
            catch (Exception e) when (e is WebException || e is MagickException)
            {
                await ctx.EditBasicAsync("Error while loading image.");
                return;
            }

            // Default 2 to 1, and 3 to 2.
            if (input2 == null)
            {
                image2 = image1;
                input2 = input1;
            }
            if (input3 == null)
            {
                image3 = image2;
                input3 = input2;
            }

            var egg = variant switch
            {
                GruVariant.Gru => false,
                GruVariant.Eggman => true,
                _ => throw new NotImplementedException($"unknown {nameof(GruVariant)}")
            };

            using var template =
                new MagickImage(
                    $"Resource/Files/memegen/grusmasterplan{(egg ? "egg" : "")}{(hyper ? "hyper" : "")}.png");

            if (image1 == null) DrawText(input1, template, egg ? 493 : 436, 114);
            else DrawImage(image1, template, egg ? 493 : 436, 114);

            if (image2 == null) DrawText(input2, template, egg ? 1343 : 1191, 114);
            else DrawImage(image2, template, egg ? 1343 : 1191, 114);

            if (image3 == null)
            {
                DrawText(input3, template, egg ? 491 : 442, 595);
                DrawText(input3, template, egg ? 1347 : 1190, 595);
                if (hyper) DrawText(input3, template, egg ? 918 : 786, 1073);
            }
            else
            {
                DrawImage(image3, template, egg ? 491 : 442, 595);
                DrawImage(image3, template, egg ? 1347 : 1190, 595);
                if (hyper) DrawImage(image3, template, egg ? 918 : 786, 1073);
            }

            await using var result = new MemoryStream(template.ToByteArray());
            
            await ctx.EditResponseAsync(
                new DiscordWebhookBuilder().AddFile("masterplan.png", result));
            
            //A method in a method? Crazy.
            static void DrawImage(MagickImage image, MagickImage template, int x, int y)
            {
                template.Composite(image, x, y, CompositeOperator.SrcOver);
            }

            //Another method in a method? Crazy.
            static void DrawText(string str, MagickImage template, int x, int y) {
                var textSettings = new MagickReadSettings
                {
                    Font = "Calibri-Bold",
                    TextGravity = Gravity.Center,
                    FillColor = MagickColors.Black,
                    BackgroundColor = MagickColors.Transparent,
                    Width = 270,
                    Height = 360
                };

                using var text = new MagickImage($"caption:{PrepText(str).Replace("\\", "\\\\")}",
                    textSettings);

                template.Composite(text, x, y, CompositeOperator.SrcOver);
            }
        }
        
        [SlashCommand("johnny", "HEREEEE'S JOHNNY!"), Category(Category.Meme)]
        async Task Johnny(InteractionContext ctx,
            [Option("user", UserArgRequiredDescription)] DiscordUser target,
            [Option(AvatarArgName, AvatarArgDescription)] AvatarPreference avaPref = default)
        {
            await ctx.SendThinking();

            using var client = new WebClient();
            await using var userAvaStream = await ctx.Member.OpenReadAvatarAsync(avaPref, client);
            await using var targetAvaStream = await ((DiscordMember)target).OpenReadAvatarAsync(avaPref, client);
            if (userAvaStream == null || targetAvaStream == null)
            {
                await ctx.EditBasicAsync(AvatarFailed);
                return;
            }
            
            using var userAva = new MagickImage(userAvaStream);
            using var targetAva = new MagickImage(targetAvaStream);
            using var template = new MagickImage("Resource/Files/memegen/johnny_template.png");

            userAva.Resize(283, 283);
            targetAva.Resize(81, 71);

            template.Composite(userAva, 111, 218, CompositeOperator.DstOver);
            template.Composite(targetAva, 250, -8, CompositeOperator.SrcOver);

            await using var output = new MemoryStream(template.ToByteArray());

            await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddFile("jahnny.png", output));
        }
        
        [SlashCommand("justright", "When you need Discord to be just right..."), Category(Category.Meme)]
        async Task JustRight(InteractionContext ctx)
        {
            await ctx.SendThinking();
            await using var file =
                new FileStream(Directory.GetFiles("Resource/Files/justright").RandomElement(),
                    FileMode.Open);
            await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddFile(file));
        }

        [SlashCommandGroup("kaede", "Kaede holds up a sign, whatever the sign contains is up to you."), Category(Category.Meme)]
        class KaedeCommands : ApplicationCommandModule
        {
            [SlashCommand("image", "Kaede holds up a sign, whatever the sign contains is up to you.")]
            async Task KaedeImage(InteractionContext ctx,
                [Option("image", ImageArgDescription)] string? uriString = null,
                [Option(RebootArgName, RebootArgDescription)] bool reboot = false)
            {
                await ctx.SendThinking();

                using var image = await GetImageArg(ctx, uriString);
                if (image == null)
                {
                    await ctx.EditBasicAsync(ImageNotFound);
                    return;
                }

                image.Resize(610, 379);
                image.BackgroundColor = MagickColors.Transparent;
                image.Extent(610, 379, Gravity.Center);

                await Base(ctx, image, reboot);
            }

            [SlashCommand("text", "Kaede holds up a sign, whatever the sign contains is up to you.")]
            async Task KaedeText(InteractionContext ctx,
                [Option("text", "The text that'll be used in the meme.")] string text,
                [Option(RebootArgName, RebootArgDescription)] bool reboot = false)
            {
                await ctx.SendThinking();

                var textSettings = new MagickReadSettings
                {
                    Font = "Calibri-Bold",
                    TextGravity = Gravity.Center,
                    FillColor = MagickColors.Black,
                    BackgroundColor = MagickColors.Transparent,
                    Width = 610,
                    Height = 379
                };

                using var image = new MagickImage($"caption:{PrepText(text).Replace("\\", "\\\\")}",
                    textSettings);

                await Base(ctx, image, reboot);
            }

            private async Task Base(InteractionContext ctx, MagickImage image, bool reboot)
            {
                using var template =
                    new MagickImage(
                        $"Resource/Files/memegen/kaededab{(reboot ? "-reboot" : "")}.jpg");
                
                image.Rotate(-6.92);
                template.Composite(image, 144, 628, CompositeOperator.SrcOver);
                await using var output = new MemoryStream(template.ToByteArray());

                await ctx.EditResponseAsync(
                    new DiscordWebhookBuilder().AddFile("kaededab.png", output));
            }
        }
        
        [SlashCommand("kirb", "POYO"), Category(Category.Meme)]
        async Task Kirb(InteractionContext ctx)
        {
            await ctx.SendThinking();
            await using var file =
                new FileStream(Directory.GetFiles("Resource/Files/kirb").RandomElement(),
                    FileMode.Open);
            await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddFile(file));
        }
        
        [SlashCommand("lean", "Leans in your discord."), Category(Category.Meme)]
        async Task Lean(InteractionContext ctx) {
            await ctx.SendThinking();
            await using var file =
                new FileStream(Directory.GetFiles("Resource/Files/lean").RandomElement(),
                    FileMode.Open);
            await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddFile(file));
        }
        
        [SlashCommand("longlive", "LONG LIVE THE KING!"), Category(Category.Meme)]
        async Task LongLive(InteractionContext ctx,
            [Option("user", UserArgRequiredDescription)] DiscordUser target,
            [Option(AvatarArgName, AvatarArgDescription)] AvatarPreference avaPref = default)
        {
            await ctx.SendThinking();

            using var client = new WebClient();
            await using var userAvaStream = await ctx.Member.OpenReadAvatarAsync(avaPref, client);
            await using var targetAvaStream = await ((DiscordMember)target).OpenReadAvatarAsync(avaPref, client);
            if (userAvaStream == null || targetAvaStream == null)
            {
                await ctx.EditBasicAsync(AvatarFailed);
                return;
            }
            
            using var userAva = new MagickImage(userAvaStream);
            using var targetAva = new MagickImage(targetAvaStream);
            using var template = new MagickImage("Resource/Files/memegen/longlivetheking_template.png");

            userAva.Resize(479, 479);
            targetAva.Resize(442, 442);

            using var bg = new MagickImage(MagickColors.White, 479, 479);
            template.Composite(bg, 1026, 42, CompositeOperator.SrcOver);
            template.Composite(userAva, 1026, 42, CompositeOperator.SrcOver);
            bg.Resize(442, 442);
            template.Composite(bg, 503, 558, CompositeOperator.SrcOver);
            template.Composite(targetAva, 503, 558, CompositeOperator.SrcOver);

            await using var output = new MemoryStream(template.ToByteArray());

            await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddFile("theking.png", output));
        }
        
        [SlashCommand("luigithumb", "Gives an image Luigi's approval."), Category(Category.Meme)]
        async Task LuigiThumb(InteractionContext ctx,
            [Option("image", ImageArgDescription)] string? uriString = null)
        {
            await ctx.SendThinking();

            using var image = await GetImageArg(ctx, uriString);
            if (image == null)
            {
                await ctx.EditBasicAsync(ImageNotFound);
                return;
            }
            
            using var luigi = new MagickImage("Resource/Files/memegen/LuigiThumb.png");

            luigi.Resize((int)(image.Width * .50), (int)(image.Height * .50));
            image.Composite(luigi, image.Width - luigi.Width, image.Height - luigi.Height, CompositeOperator.SrcOver);

            await using var output = new MemoryStream(image.ToByteArray());

            await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddFile("loogi.png", output));
        }
        
        [SlashCommand("magik", "Also known as the Content Awareness Scale. ..Huh? NotSoBot didn't die? This tribute was for nothing?")]
        [Category(Category.Meme)]
        async Task Magik(InteractionContext ctx,
            [Option("image", ImageArgDescription)] string? uriString = null)
        {
            await ctx.SendThinking();

            using var image = await GetImageArg(ctx, uriString);
            if (image == null)
            {
                await ctx.EditBasicAsync(ImageNotFound);
                return;
            }

            image.Resize(800, 800);
            image.LiquidRescale(400, 400);
            image.LiquidRescale(1200, 1200);

            await using var output = new MemoryStream(image.ToByteArray());

            await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddFile("magik.png", output));
        }
        
        [SlashCommand("notallowed", "Huh. I wonder who that's for?"), Category(Category.Meme)]
        async Task NotAllowed(InteractionContext ctx,
            [Option("user", UserArgOrYouDescription)] DiscordUser? user = null,
            [Option(AvatarArgName, AvatarArgDescription)] AvatarPreference avaPref = default)
        {
            await ctx.SendThinking();

            var m = (DiscordMember)(user ?? ctx.Member);

            await using var avaStream = await m.OpenReadAvatarAsync(avaPref);
            if (avaStream == null)
            {
                await ctx.EditBasicAsync(AvatarFailed);
                return;
            }
            
            using var ava = new MagickImage(avaStream);
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
            await using var output = new MemoryStream(template.ToByteArray());

            await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddFile("huh.png", output));
        }
        
        [SlashCommand("poosy", "\"Poosy...De...stroyer.\" ~Vinesauce Joel"), Category(Category.Meme)]
        async Task Poosy(InteractionContext ctx,
            [Option(RebootArgName, RebootArgDescription)] bool reboot = false)
        {
            await ctx.SendThinking();
            await using var file =
                new FileStream($"Resource/Files/memegen/poosy{(reboot ? "-reboot" : "")}.png",
                    FileMode.Open);
            await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddFile(file));
        }
        
        [SlashCommand("poster", "If only I had a Krabby Patty poster instead..."), Category(Category.Meme)]
        async Task Poster(InteractionContext ctx,
            [Option("image", ImageArgDescription)] string? uriString = null)
        {
            await ctx.SendThinking();

            using var image = await GetImageArg(ctx, uriString);
            if (image == null)
            {
                await ctx.EditBasicAsync(ImageNotFound);
                return;
            }
            
            using var template = new MagickImage("Resource/Files/memegen/lick.png");

            image.Resize(309, 225);
            image.BackgroundColor = MagickColors.White;
            image.VirtualPixelMethod = VirtualPixelMethod.Background;
            image.Extent(309, 225, Gravity.Center);
            var settings = new DistortSettings
            {
                Viewport = new MagickGeometry(template.Width, template.Height)
            };
            //Distort the image to match the perspective of the template.
            //Pattern: (Src TL X, Src TL Y, Dest TL X, Dest TL Y, Src TR X, Src TR Y, Dest TR X, Dest TR Y, Src BR X, Src BR Y, Dest BR X, Dest BR Y, Src BL X, Src BL Y, Dest BR X, Dest BR Y)
            image.Distort(DistortMethod.Perspective, settings, 0, 0, 291, 0, image.Width, 0, 599, 0, image.Width, image.Height, 599, 253, 0, image.Height, 291, 224);
            template.Composite(image, CompositeOperator.DstOver);
            await using var output = new MemoryStream(template.ToByteArray());

            await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddFile("poster.png", output));
        }
        
        [SlashCommand("switch", "Shows how easy it is to setup a switch, with a twist."), Category(Category.Meme)]
        async Task Switch(InteractionContext ctx,
            [Option("image", ImageArgDescription)] string? uriString = null)
        {
            await ctx.SendThinking();

            using var image = await GetImageArg(ctx, uriString);
            if (image == null)
            {
                await ctx.EditBasicAsync(ImageNotFound);
                return;
            }
                
            using var template = new MagickImage("Resource/Files/memegen/switch_setup.png");

            image.Resize(174, 157);
            image.BackgroundColor = MagickColors.Transparent;
            image.Extent(174, 157, Gravity.Center);
            template.Composite(image, 366, 214, CompositeOperator.DstOver);

            await using var output = new MemoryStream(template.ToByteArray());

            await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddFile("swatch.png", output));
        }
        
        [SlashCommand("technology", "\"We have T E C H N O L O G Y\" ~Patrick"), Category(Category.Meme)]
        async Task Technology(InteractionContext ctx,
            [Option("image", ImageArgDescription)] string? uriString = null)
        {
            await ctx.SendThinking();

            using var image = await GetImageArg(ctx, uriString);
            if (image == null)
            {
                await ctx.EditBasicAsync(ImageNotFound);
                return;
            }
            
            using var template = new MagickImage("Resource/Files/memegen/wehavetechnology.png");

            image.Resize(335, 286);
            image.BackgroundColor = MagickColors.White;
            image.Extent(335, 286, Gravity.Center);
            template.Composite(image, 37, 555, CompositeOperator.DstOver);
            await using var output = new MemoryStream(template.ToByteArray());

            await ctx.EditResponseAsync(
                new DiscordWebhookBuilder().AddFile("T E C H N O L O G Y.png", output));
        }
        
        [SlashCommand("torture", "The worst torture possible."), Category(Category.Meme)]
        async Task Torture(InteractionContext ctx,
            [Option("image", ImageArgDescription)] string? uriString = null)
        {
            await ctx.SendThinking();

            using var image = await GetImageArg(ctx, uriString);
            if (image == null)
            {
                await ctx.EditBasicAsync(ImageNotFound);
                return;
            }
                
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
            await using var output = new MemoryStream(template.ToByteArray());

            await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddFile("torture.png", output));
        }
        
        [SlashCommand("trash", "This piece of trash was mistaken for art?"), Category(Category.Meme)]
        async Task Trash(InteractionContext ctx,
            [Option("image", ImageArgDescription)] string? uriString = null)
        {
            await ctx.SendThinking();

            using var image = await GetImageArg(ctx, uriString);
            if (image == null)
            {
                await ctx.EditBasicAsync(ImageNotFound);
                return;
            }
                
            using var clone = image.Clone();
            using var template = new MagickImage("Resource/Files/memegen/trash.png");

            image.Resize(382, 262);
            image.BackgroundColor = MagickColors.White;
            image.VirtualPixelMethod = VirtualPixelMethod.Background;
            image.Extent(382, 262, Gravity.Center);
            var settings = new DistortSettings
            {
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
            await using var output = new MemoryStream(template.ToByteArray());

            await ctx.EditResponseAsync(
                new DiscordWebhookBuilder().AddFile("trash_but_its_not.png", output));
        }
        
        [SlashCommand("trashwaifu", "Your waifu is entry level garbage!"), Category(Category.Meme)]
        async Task TrashWaifu(InteractionContext ctx,
            [Option("image", ImageArgDescription)] string? uriString = null)
        {
            await ctx.SendThinking();

            using var image = await GetImageArg(ctx, uriString);
            if (image == null)
            {
                await ctx.ReplyBasicAsync(ImageNotFound);
                return;
            }
                
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

            await using var output = new MemoryStream(template.ToByteArray());

            await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddFile("trash.png", output));
        }
        
        [SlashCommand("triggered", "I'm T R I G G E R E D"), Category(Category.Meme)]
        async Task Triggered(InteractionContext ctx,
            [Option("user", UserArgOrYouDescription)] DiscordUser? user = null,
            [Option(AvatarArgName, AvatarArgDescription)] AvatarPreference avaPref = default)
        {
            await ctx.SendThinking();

            var m = (DiscordMember)(user ?? ctx.Member);

            await using var avaStream = await m.OpenReadAvatarAsync(avaPref);
            if (avaStream == null)
            {
                await ctx.EditBasicAsync(AvatarFailed);
                return;
            }
            using var ava = new MagickImage(avaStream);
            ava.Resize(500, 500);
            using var canvas = new MagickImage(MagickColor.FromRgba(0, 0, 0, 0), 500, 500);
            canvas.Format = MagickFormat.Gif;
            using var triggered = new MagickImage("Resource/Files/memegen/triggered.png");
            using var overlay = new MagickImage("Resource/Files/memegen/triggered_overlay.png");
            using var gif = new MagickImageCollection();
            for (var i = 0; i < 10; i++) {
                gif.Add(canvas.Clone());
                gif[i].AnimationDelay = 3;
                gif[i].Composite(ava, Random.Next(-30, 30), Random.Next(-20, 20), CompositeOperator.SrcOver);
                gif[i].Composite(triggered, Random.Next(-30, 30), 327 + Random.Next(-20, 20), CompositeOperator.SrcOver);
                gif[i].Composite(overlay, CompositeOperator.SrcOver);
                gif[i].GifDisposeMethod = GifDisposeMethod.Previous;
            }

            var settings = new QuantizeSettings
            {
                Colors = 256
            };
            gif.Quantize(settings);
            gif.Coalesce();

            await using var stream = new MemoryStream(gif.ToByteArray());

            await ctx.EditResponseAsync(
                new DiscordWebhookBuilder().AddFile("triggered.gif", stream));
        }
        
        [SlashCommand("urgent", "If this is urgent, reply \"urgent\"..."), Category(Category.Meme)]
        async Task Urgent(InteractionContext ctx,
            [Option("image", ImageArgDescription)] string? uriString = null)
        {
            await ctx.SendThinking();
            
            using var image = await GetImageArg(ctx, uriString);
            if (image == null)
            {
                await ctx.ReplyBasicAsync(ImageNotFound);
                return;
            }
                
            using var template = new MagickImage("Resource/Files/memegen/urgent.png");
            using var bg = new MagickImage(MagickColors.Black, 552, 465);

            image.Resize(552, 465);
            image.BackgroundColor = MagickColors.Transparent;
            image.Extent(552, 465, Gravity.Center);
            template.Composite(image, 22, 11, CompositeOperator.DstOver);
            template.Composite(bg, 22, 11, CompositeOperator.DstOver);

            await using var output = new MemoryStream(template.ToByteArray());

            await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddFile("urgent.png", output));
        }
        
        [SlashCommand("www", "Thanks to the miracle of the world wide web, I can search anything I want!"), Category(Category.Meme)]
        async Task WWW(InteractionContext ctx,
            [Option("image", ImageArgDescription)] string? uriString = null)
        {
            await ctx.SendThinking();

            using var image = await GetImageArg(ctx, uriString);
            if (image == null)
            {
                await ctx.EditBasicAsync(ImageNotFound);
                return;
            }
                
            using var template = new MagickImage("Resource/Files/memegen/www.png");

            image.Resize(165, 131);
            image.BackgroundColor = MagickColors.Transparent;
            image.Extent(165, 131, Gravity.Center);
            template.Composite(image, 132, 466, CompositeOperator.DstOver);

            await using var output = new MemoryStream(template.ToByteArray());

            await ctx.EditResponseAsync(
                new DiscordWebhookBuilder().AddFile("marvelous.png", output));
        }

        [SlashCommand("youtried", "You tried. Here's a gold star!"), Category(Category.Meme)]
        async Task YouTried(InteractionContext ctx)
        {
            await ctx.SendThinking();
            await using var file =
                new FileStream(Directory.GetFiles("Resource/Files/youtried").RandomElement(),
                    FileMode.Open);
            await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddFile(file));
        }

    }
    
    public class MemeCommandsOld : BaseCommandModule {

        private readonly Randumb Random = Randumb.Instance;

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
