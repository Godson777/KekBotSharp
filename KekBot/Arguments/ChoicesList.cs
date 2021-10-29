using System.Linq;

namespace KekBot.Arguments {
    internal struct ChoicesList {
        internal readonly string[] Choices;

        private ChoicesList(string[] choices) => Choices = choices;

        internal static ChoicesList Parse(string value)
        {
            var choices = SplitChoices(value, "|");
            if (choices.Length == 1)
                choices = StripOr(SplitChoices(choices.Single(), ","));
            if (choices.Length == 1)
                choices = SplitChoices(choices.Single(), " ");
            return new ChoicesList(choices);
        }
        
        private static string[] SplitChoices(string choicesString, string sep) => choicesString
            .Split(sep)
            .Select(s => s.Trim())
            .Where(s => s.Length > 0)
            .ToArray();

        /// <summary>
        /// Removes "or " from the last element of choices.
        /// </summary>
        private static string[] StripOr(string[] choices) {
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
