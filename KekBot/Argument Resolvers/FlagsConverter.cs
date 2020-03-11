using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Converters;
using DSharpPlus.Entities;
using KekBot.Arguments;
using KekBot.Utils;

namespace KekBot.ArgumentResolvers {
    class FlagsConverter : IArgumentConverter<Flags> {
        [SuppressMessage("Globalization", "CA1308:Normalize strings to uppercase", Justification = "The bot is written in English, shut up")]
        public Task<Optional<Flags>> ConvertAsync(string value, CommandContext ctx) => Task.FromResult(Optional.FromValue(new Flags(
                value
                    .SplitOnWhitespace()
                    .Where(s => s.Length >= 2 && s.First() == '-')
                    .Select(arg => TrimHyphens(arg).Split('='))
                    .Where(splits => splits.Length <= 2)
                    .Select(splits => (
                        flagName: splits[0].ToLowerInvariant(),
                        flagValue: string.IsNullOrEmpty(splits.ElementAtOrDefault(1)) ? splits[0] : splits[1]
                    ))
                    .ToDicktionary()
        )));

        private static string TrimHyphens(string s) => s.Substring(s[1] == '-' ? 2 : 1);

    }
}
