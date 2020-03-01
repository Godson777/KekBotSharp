using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace KekBot
{
    public class PickCommand : BaseCommandModule
    {

        private static System.Random rng = new System.Random();

        [Command("pick"), Aliases("choose", "decide"),
            Description("Has KekBot pick one of X choices for you."),
            Cooldown(10, 1.0, CooldownBucketType.User)]
        public async Task Pick(CommandContext ctx,
            [Description("Options separated with bars (`|`s), commas (`,`s), or spaces (for single-word choices).")]
            ChoicesList choicesStruct = new ChoicesList())
        {
            var choices = choicesStruct.Choices;
            Debug.Assert(choices.Length >= 0, "Negative length? wut");

            await ctx.RespondAsync(choices.Length switch
            {
                0 => "You haven't given me any choices, though...",
                1 => $"Well, I guess I'm choosing `{choices[0]}`, since you haven't given me anything else to pick...",
                _ => $"Hm... I think I'll go with `{randElement(choices)}`.",
            });
        }

        private T randElement<T>(IEnumerable<T> list) => list.ElementAt(rng.Next(list.Count()));

        public struct ChoicesList
        {
            public readonly string[] Choices;
            public ChoicesList(string[] choices) => Choices = choices;
        }
    }
}
