using System.Linq;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Converters;
using DSharpPlus.Entities;
using static KekBot.PickCommand;

namespace KekBot.ArgumentResolvers {
    class ChoicesConverter : IArgumentConverter<ChoicesList> {

        public Task<Optional<ChoicesList>> ConvertAsync(string value, CommandContext ctx) {
            var choices = SplitChoices(value, "|");
            if (choices.Length == 1)
                choices = StripOr(SplitChoices(choices[0], ","));
            if (choices.Length == 1)
                choices = SplitChoices(choices[0], " ");
            return Task.FromResult(Optional.FromValue(
                new ChoicesList(choices)
            ));
        }

        private string[] SplitChoices(string choicesString, string sep) => choicesString
            .Split(sep)
            .Select(s => s.Trim())
            .Where(s => s.Length > 0)
            .ToArray();

        /// <summary>
        /// Removes "or " from the last element of choices.
        /// </summary>
        private string[] StripOr(string[] choices) {
            const string or = "or ";

            if (choices.Length >= 2) {
                var last = choices[^1];
                if (last.StartsWith(or, System.StringComparison.Ordinal)) {
                    last = last.Remove(0, or.Length);
                    choices[^1] = last;
                }
            }

            return choices;
        }

    }
}
