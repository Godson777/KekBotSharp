using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using System;
using System.Threading.Tasks;
using System.Linq;
using KekBot.Arguments;
using KekBot.Utils;

namespace KekBot.Commands {
    class PickCommand : BaseCommandModule {

        [Command("pick"), Aliases("choose", "decide"), Description("Has KekBot pick one of X choices for you.")]
        async Task Pick(
            CommandContext ctx,
            [RemainingText, Description("Options separated with vertical bars, commas, or just spaces (for single-word choices).")]
            ChoicesList? choices = null
        ) {
            var choicesArray = choices?.Choices ?? Array.Empty<string>();
            await ctx.RespondAsync(choicesArray.Length switch {
                0 => "You haven't given me any choices, though...",
                1 => $"Well, I guess I'm choosing `{choicesArray.Single()}`, since you haven't given me anything else to pick...",
                _ => $"Hm... I think I'll go with `{choicesArray.RandomElement()}`.",
            });
        }

    }
}
