using System.Collections.Generic;

namespace KekBot.Arguments {
    internal struct Flags {
        private readonly Dictionary<string, string>? NamesToValues;

        public Flags(Dictionary<string, string> flags) {
            NamesToValues = flags;
        }

        /// <summary>
        /// Returns true if the flag was given, no matter whether a value was given or not.
        /// </summary>
        public bool Has(string flagName) =>
            NamesToValues != null && NamesToValues.ContainsKey(flagName);

        /// <summary>
        /// Returns true if the flag is present and not empty.
        /// </summary>
        public bool IsNonEmpty(string flagName) =>
            GetNonEmpty(flagName) != null;

        /// <summary>
        /// Returns the flag's value (or the empty string if it doesn't exist).
        /// </summary>
        public string GetOrEmpty(string flagName) =>
            NamesToValues != null && NamesToValues.TryGetValue(flagName, out var value)
                ? value
                : "";

        /// <summary>
        /// Returns the flag's value if it exists and is not empty, else returns null.
        /// </summary>
        public string? GetNonEmpty(string flagName) {
            var value = GetOrEmpty(flagName);
            return value.Length == 0 ? null : value;
        }
    }
}
