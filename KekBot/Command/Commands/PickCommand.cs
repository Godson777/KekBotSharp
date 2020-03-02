using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using System.Diagnostics;
using System.Threading.Tasks;
using KekBot.Util;

namespace KekBot
{
    public class PickCommand : BaseCommandModule
    {

        [Command("pick"), Aliases("choose", "decide")]
        [Description("Has KekBot pick one of X choices for you.")]
        [Cooldown(10, 1.0, CooldownBucketType.User)]
        [Priority(0)]
        public async Task Pick(CommandContext ctx,
            [Description("Options separated with vertical bars, commas, or just spaces (for single-word choices).")]
            [RemainingText]
            ChoicesList? choices = null)
        {
            var choicesArray = choices?.Choices ?? new string[] { };

            Debug.Assert(choicesArray.Length >= 0, "negative length?");

            await ctx.RespondAsync(choicesArray.Length switch
            {
                0 => "You haven't given me any choices, though...",
                1 => $"Well, I guess I'm choosing `{choicesArray[0]}`, since you haven't given me anything else to pick...",
                _ => $"Hm... I think I'll go with `{choicesArray.RandomElement()}`.",
            });
        }

        public struct ChoicesList
        {
            public readonly string[] Choices;
            public ChoicesList(string[] choices) => Choices = choices;
        }
    }
}
