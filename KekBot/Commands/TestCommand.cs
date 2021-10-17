using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Extensions;
using ImageMagick;
using ImageMagick.Defines;
using KekBot.Menu;
using KekBot.Profiles;
using KekBot.Profiles.Item;
using Microsoft.VisualBasic;
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

            /*await ctx.TriggerTypingAsync();

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
            }*/

            //var profile = await LegacyProfile.getProfile(ctx.User);
            //await ctx.RespondAsync(profile.SpitDebugInfo());

            var options = new List<DiscordSelectComponentOption>();
            options.Add(new DiscordSelectComponentOption("yo waddup im option 1", "option_1", "lol this is a description"));
            options.Add(new DiscordSelectComponentOption("hi im option 2", "option_2", "wait this is a description?"));
            options.Add(new DiscordSelectComponentOption("hey there im option 3", "option_3", "woah this is a description"));
            options.Add(new DiscordSelectComponentOption("option 4, this is.", "option_4", "a description, this is"));
            options.Add(new DiscordSelectComponentOption("im 5", "option_5", "wheeeeeeeeeee"));
            var msg = await ctx.RespondAsync(new DiscordMessageBuilder().WithContent("This is a test message.").AddComponents(new DiscordSelectComponent("test_select", "Yo this is a test placeholder lol", options, false, 1, 5)).WithReply(ctx.Message.Id));
            var result = await msg.WaitForSelectAsync(ctx.User, "test_select", TimeSpan.FromSeconds(30));
            if (result.TimedOut) {
                await msg.ModifyAsync(new DiscordMessageBuilder().WithContent("YOU TOOK TOO LONG DUMBASS."));
            }

            if (result.Result.Values[0] == "option_1") {
                await result.Result.Interaction.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent("lol u picked option 1").AsEphemeral(true));
            }
        }

        [Command("testitem")]
        public async Task TestItem(CommandContext ctx) {
            var bg = Background.New("test", "Test Background", "MARIO_GALAXY.png");
            await ItemRegistry.Get.AddItem(bg);
            await ctx.RespondAsync("Test Item Added");
        }

        [Command("gettestitem")]
        public async Task GetTestItem(CommandContext ctx) {
            var bg = ItemRegistry.Get.GetItemByID("test");
            await ctx.RespondAsync($"DEBUG MESSAGE\n" +
                $"Item Type:  {bg?.Tag}\n" +
                $"Item ID: {bg?.ID}\n" +
                $"Item Name: {bg?.Name}\n" +
                $"That's all I care to test rn");
            await new DiscordMessageBuilder().WithFile(new FileStream($"Resource/Files/profile/background/{bg?.File}", FileMode.Open)).SendAsync(ctx.Channel);
        }
    }
}
