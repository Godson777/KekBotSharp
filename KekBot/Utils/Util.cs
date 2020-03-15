using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Entities;
using DSharpPlus.Entities;

namespace KekBot.Utils {
    static class Util {

        internal static readonly Random Rng = new Random();

        /// <summary>
        /// Returns a random element from the collection.
        /// </summary>
        internal static T RandomElement<T>(this IEnumerable<T> coll) => coll.ElementAt(Rng.Next(coll.Count()));

        /// <summary>
        /// Returns null if the collection is empty.
        /// </summary>
        internal static IEnumerable<T>? NonEmpty<T>(this IEnumerable<T> coll) => coll.Any() ? coll : null;

        // string is IEnumerable<char>, but without this overload, the return is an inconvenient type.
        /// <summary>
        /// Returns null if the string is empty.
        /// </summary>
        internal static string? NonEmpty(this string s) => s.Length == 0 ? null : s;

        /// <summary>
        /// Appends copies of the specified strings followed by the default line terminator
        /// to the end of the current System.Text.StringBuilder object.
        /// </summary>
        /// <param name="lines">The strings to append.</param>
        /// <returns>A reference to this instance after the append operation has completed.</returns>
        internal static StringBuilder AppendLines(this StringBuilder s, IEnumerable<string> lines) {
            foreach (var line in lines) s.AppendLine(line);
            return s;
        }

        /// <summary>
        /// Use an arg resolver to parse an argument.
        /// </summary>
        internal static async Task<Arg?> ConvertArgAsync<Arg>(string value, CommandContext ctx) where Arg : class {
            try {
                // God, this method sucks. And there's no alternative, as far as I can tell;
                // the property that contains the registered converters is private.
                return (Arg)await ctx.CommandsNext.ConvertArgument<Arg>(value, ctx);
            } catch (ArgumentException e) {
                if (e.Message != "Could not convert specified value to given type. (Parameter 'value')") {
                    Console.WriteLine($"Caught error from ConvertArgument: {e}");
                }
                return null;
            }
        }
        /// <summary>
        /// Use an arg resolver to parse an argument. Just make sure to include whatever it needs.
        /// </summary>
        internal static Task<Arg?> ConvertArgAsync<Arg>(string value, CommandsNextExtension cnext,
            DiscordMessage? msg = null, string prefix = "", Command? cmd = null, string? rawArgs = null)
            where Arg : class => ConvertArgAsync<Arg>(value,
                cnext.CreateContext(msg: msg, prefix: prefix, cmd: cmd, rawArguments: rawArgs));

        internal static T? ToNullableClass<T>(this Optional<T> opt)
            where T : class => opt.HasValue ? opt.Value : null;

        internal static T? ToNullable<T>(this Optional<T> opt)
            where T : struct => opt.HasValue ? (T?)opt.Value : null;

        internal static Optional<T> ToOptional<T>(this T? maybeValue) where T : struct =>
            maybeValue is T value ? Optional.FromValue(value) : Optional.FromNoValue<T>();

        internal static Optional<T> ToOptional<T>(this T? maybeValue) where T : class =>
            maybeValue is T value ? Optional.FromValue(value) : Optional.FromNoValue<T>();

        /// <summary>
        /// Returns the index if it was found (nonnegative), else a fallback int.
        /// </summary>
        internal static int FoundIndexOr(this int n, int fallback) => n < 0 ? fallback : n;

        /// <summary>
        /// Returns the zero-based index of <em>the end of</em> the first occurrence of the specified string.
        /// </summary>
        internal static int IndexOfEnd(this string s, string value, StringComparison comparisonType) {
            var i = s.IndexOf(value, comparisonType);
            return i < 0
                ? i
                : i + value.Length;
        }

        /// <summary>
        /// Returns the zero-based index of <em>the end of</em> the first occurrence of the specified string.
        /// </summary>
        internal static int IndexOfEnd(this string s, string value, int startIndex, StringComparison comparisonType) {
            var i = s.IndexOf(value, startIndex, comparisonType);
            return i < 0
                ? i
                : i + value.Length;
        }

        /// <summary>
        /// Returns the zero-based index of <em>the end of</em> the first occurrence of the specified string,
        /// according to binary comparison.
        /// </summary>
        internal static int FastIndexOfEnd(this string s, string value, int startIndex = 0) =>
            s.IndexOfEnd(value, startIndex, StringComparison.Ordinal);

        internal static string AuthorName(this DiscordMessage msg) => msg.Author switch {
            DiscordMember m => m.DisplayName,
            DiscordUser u => u.Username,
        };

        internal static string GetRawArgString(this CommandContext ctx, string cmdName) =>
            ctx.Message.GetRawArgString(ctx.Prefix, cmdName);

        internal static string GetRawArgString(this DiscordMessage msg, string prefix, string cmdName) {
            var content = msg.Content;
            var afterPrefix = content.FastIndexOfEnd(prefix).FoundIndexOr(0);
            var afterCmd = content.FastIndexOfEnd(cmdName, afterPrefix).FoundIndexOr(0);
            return content.Substring(afterCmd).Trim();
        }

        // TODO: I forgot about this; remove it I guess.
        /// <summary>
        /// Get a command module of the given type from CNext.RegisteredCommands. Panics if it can't be found.
        /// </summary>
        internal static T GetModule<T>(this IReadOnlyDictionary<string, DSharpPlus.CommandsNext.Command> cmds)
            where T : BaseCommandModule =>
                cmds.Values.First(cmd => cmd.Module.ModuleType == typeof(T)).Module is T mod
                    ? mod
                    : throw Panic("couldn't get module");

        internal static IEnumerable<int> Range(int start = 0, int end = int.MaxValue, int step = 1) {
            for (int n = start; n < end; n += step) {
                yield return n;
            }
        }

        internal static ImmutableArray<T> ImmutableArrayFromSingle<T>(T value) => ImmutableArray.Create(new[] { value });

        internal static int ParseInt(string intStr, int fallback) => int.TryParse(intStr, out var n) ? n : fallback;


        /// <summary>
        /// This only exists for type inference.
        /// </summary>
        [Serializable]
        public class PanicException : Exception {
            public PanicException() { }
            public PanicException(string message) : base(message) { }
            public PanicException(string message, Exception inner) : base(message, inner) { }
            protected PanicException(
              System.Runtime.Serialization.SerializationInfo info,
              System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
        }

        /// <summary>
        /// Terminate the program, optionally with a message.
        /// Throw the returned exception to get better type inference from C#.
        /// </summary>
        internal static PanicException Panic(string msg = "") {
            Console.Error.WriteLine(msg);
            Environment.Exit(1);
            return new PanicException(message: msg);
        }

        /// <summary>
        /// Asserts a condition is true, and panics otherwise.
        /// </summary>
        internal static void Assert(bool condition, string elsePanicWith = "") {
            if (!condition) {
                Panic(elsePanicWith);
            }
        }

        /// <summary>
        /// Converts the string to a fixed-width string.
        /// </summary>
        /// <param name="s">String to fix the width of.</param>
        /// <param name="targetLength">Length that the string should be.</param>
        /// <returns>Adjusted string.</returns>
        public static string ToFixedWidth(this string s, int targetLength) {
            if (s == null)
                throw new NullReferenceException();

            if (s.Length < targetLength)
                return s.PadRight(targetLength, ' ');

            if (s.Length > targetLength)
                return s.Substring(0, targetLength);

            return s;
        }

    }
}
