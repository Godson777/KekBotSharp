using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using KekBot.Utils;

namespace KekBot.Arguments {
    internal struct FlagArgs {

        /// <summary>
        /// Parses things like "--nsfw=only --tags=bdsm"
        /// </summary>
        private static readonly Regex FlagRegex = new Regex(
            "(?<!\\S)-{1,2}(?<name>[a-zA-Z]+)(?:[:=](?<value>\\S+))(?!\\S)"
        );

        private static readonly string[] TruthyValues = new[] { "1", "ON", "YES", "TRUE", "ENABLE", "ENABLED" };
        private static readonly string[] FalsyValues = new[] { "0", "OFF", "NO", "FALSE", "DISABLE", "DISABLED" };

        /// <summary>
        /// Flag names to their values. It's null if you instantiate the default value of this struct, ofc.
        /// </summary>
        public readonly Dictionary<string, string>? Flags;

        /// <summary>
        /// Parse flags from a string.
        /// </summary>
        public static FlagArgs? ParseString(string s) {
            var flags = FlagRegex.Matches(s).ToDictionary(
                match => match.Groups["name"].Value.ToUpperInvariant(),
                match => match.Groups["value"].Value.NonEmpty() ?? match.Groups["name"].Value
            );
            return flags.Count == 0 ? null : new FlagArgs(flags) as FlagArgs?;
        }

        /// <summary>
        /// Parse flags from a string, storing the non-flag parts into <paramref name="sWithoutFlags"/>.
        /// </summary>
        public static FlagArgs? ParseString(string s, out string sWithoutFlags) {
            var matches = FlagRegex.Matches(s);
            sWithoutFlags = RemoveFlagsFrom(s, matches).Trim();
            var flags = matches.ToDictionary(
                match => match.Groups["name"].Value.ToUpperInvariant(),
                match => match.Groups["value"].Value.NonEmpty() ?? match.Groups["name"].Value
            );
            return flags.Count == 0 ? null : new FlagArgs(flags) as FlagArgs?;
        }

        // There'll only be a few flags 99% of the time, so not worth the overhead of a string builder.
        private static string RemoveFlagsFrom(string s, MatchCollection flagMatches) =>
            // Starting from the right side so the indices don't change when I remove substrings.
            flagMatches.Reverse().Aggregate(s, (ss, match) => ss.Remove(match.Index, match.Length));

        private FlagArgs(Dictionary<string, string> flags) => Flags = flags;

        /// <summary>
        /// Returns true if the flag was given, no matter whether a value was given or not.
        /// </summary>
        public bool Has(string flagName) =>
            Flags != null && Flags.ContainsKey(flagName.ToUpperInvariant());

        /// <summary>
        /// Returns the flag's value (or the empty string if it doesn't exist).
        /// </summary>
        public string Get(string flagName) =>
            Flags != null && Flags.TryGetValue(flagName.ToUpperInvariant(), out var value)
                ? value
                : "";

        /// <summary>
        /// Returns true if the flag is some truthy value, false if it's some falsy value, and null otherwise.
        /// </summary>
        public bool? ParseBool(string flagName) {
            flagName = flagName.ToUpperInvariant();
            var value = Get(flagName).ToUpperInvariant();
            if (value == flagName || TruthyValues.Contains(value)) return true;
            if (FalsyValues.Contains(value)) return false;
            return null;
        }

        /// <summary>
        /// Tries to parse the flag value as an enum. If it fails, returns null.
        /// </summary>
        public T? ParseEnum<T>(string flagName) where T : struct =>
            Enum.TryParse(Get(flagName), ignoreCase: true, out T result) ? result as T? : null;

    }
}
