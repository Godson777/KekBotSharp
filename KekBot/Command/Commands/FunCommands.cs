using System;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using KekBot.Attributes;
using KekBot.Utils;

namespace KekBot.Command.Commands {
    public class FunCommands : BaseCommandModule {

        private static readonly string[] EightBall = { "It is certain.", "It is decidedly so.", "Yes, definitely.", "You may rely on it.",
        "As I see it, yes.", "Most likely.", "Outlook good.", "Yes.", "Signs point to yes.", "Reply hazy, try again.", "Ask again later.",
        "Better not tell you now.", "Cannot predict now.", "Concentrate and ask again.", "Don't count on it.", "My reply is no.", "My sources say no.",
        "Outlook not so good.", "Very doubtful." };
        private static Random Random = new Random();

        [Command("8ball"), Description("Ask the magic 8-ball a question!"), Category(Category.Fun)]
        public async Task EightBallCommand(CommandContext ctx, [RemainingText, Description("The question to ask to the magic 8-ball.")] string question) {
            CustomEmote emote = await CustomEmote.Get();
            if (question == null) {
                await ctx.RespondAsync($"{emote.Think} I asked: Did {ctx.User.Username} give you a question?\n\n🎱 8-Ball's response: No, they didn't.");
            } else {
                await ctx.RespondAsync($"{emote.Think} You asked: {question}\n\n🎱 8-Ball's response: {EightBall.RandomElement()}");
            }
        }

        [Command("avatar"), Description("Sends a larger version of the specified user's avatar.")]
        [Aliases("ava"), Category(Category.Fun), Priority(0)]
        public async Task AvatarCommand(CommandContext ctx, [Description("The user to pull the avatar from."), Required] DiscordMember? user = null) {
            if (user == null) await ctx.RespondAsync("peepee");
            else await ctx.RespondAsync(user.AvatarUrl);
        }

        [Command("flip"), Description("Flips a coin."), Category(Category.Fun)]
        public async Task FlipCommand(CommandContext ctx) {
            string coin = Random.Next(2) == 0 ? "HEADS" : "TAILS";
            await ctx.RespondAsync($"{ctx.User.Username} flipped the coin and it landed on... ***{coin}!***");
        }
        

    }
}
