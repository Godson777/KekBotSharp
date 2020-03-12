using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;

namespace KekBot.Utils {
    static class Util {

        internal static readonly Random Rng = new Random();

        internal static T RandomElement<T>(this IEnumerable<T> list) => list.ElementAt(Rng.Next(list.Count()));

        internal static IEnumerable<T>? NonEmpty<T>(this IEnumerable<T> list) => list.Any() ? list : null;
        // string is IEnumerable<char>, but without this overload, the return is an inconvenient type.
        internal static string? NonEmpty(this string s) => s.Length == 0 ? null : s;

        // Idk, just disable the warning.
#pragma warning disable CS8714 // The type cannot be used as type parameter in the generic type or method. Nullability of type argument doesn't match 'notnull' constraint.
        internal static Dictionary<K, V> ToDicktionary<K, V>(this IEnumerable<(K, V)> pairs) =>
#pragma warning restore CS8714 // The type cannot be used as type parameter in the generic type or method. Nullability of type argument doesn't match 'notnull' constraint.
            pairs.ToDictionary(pair => pair.Item1, pair => pair.Item2);

        /// <summary>
        /// Appends copies of the specified strings followed by the default line terminator
        /// to the end of the current System.Text.StringBuilder object.
        /// </summary>
        /// <param name="s">The current System.Text.StringBuilder object/param>
        /// <param name="lines">The strings to append.</param>
        /// <returns>A reference to this instance after the append operation has completed.</returns>
        internal static StringBuilder AppendLines(this StringBuilder s, IEnumerable<string> lines) {
            foreach (var line in lines) s.AppendLine(line);
            return s;
        }

        internal static string[] SplitOnWhitespace(this string s, StringSplitOptions options = StringSplitOptions.RemoveEmptyEntries) =>
            s.Split(null as char[], options);

        internal static async Task<Arg?> ConvertArgAsync<Arg>(this string value, CommandContext ctx) where Arg : class {
            try {
                // God, this method sucks. And there's no alternative, as far as I can tell;
                // the property that contains the registered converters is private.
                return (Arg)await ctx.CommandsNext.ConvertArgument<Arg>(value, ctx);
            } catch (ArgumentException e) when (e.Message == "Could not convert specified value to given type.") {
                return default;
            }
        }

        internal static T? ToNullableClass<T>(this Optional<T> opt)
            where T : class => opt.HasValue ? opt.Value : null;

        internal static T? ToNullable<T>(this Optional<T> opt)
            where T : struct => opt.HasValue ? (T?)opt.Value : null;

        internal static Optional<T> ToOptional<T>(this T? maybeValue) where T : struct =>
            maybeValue is T value ? Optional.FromValue(value) : Optional.FromNoValue<T>();

        internal static Optional<T> ToOptional<T>(this T? maybeValue) where T : class =>
            maybeValue is T value ? Optional.FromValue(value) : Optional.FromNoValue<T>();

        internal static Task<T?> NonNull<T>(this Task<T?>? task) where T : class => task ?? Task.FromResult<T?>(null);

        internal static string AuthorName(this DiscordMessage msg) => msg.Author switch {
            DiscordMember m => m.DisplayName,
            DiscordUser u => u.Username,
        };

        //internal static Lib.CommandsKextExtension UseCommandsKext(this DiscordClient client, CommandsNextConfiguration cfg) {
        //    if (client.GetExtension<Lib.CommandsKextExtension>() != null)
        //        throw new InvalidOperationException("CommandsKext is already enabled for that client.");

        //    var cnext = new Lib.CommandsKextExtension(cfg);
        //    client.AddExtension(cnext);
        //    return cnext;
        //}

        internal static IEnumerable<int> Range(int start = 0, int end = int.MaxValue, int step = 1) {
            for (int n = start; n < end; n += step) {
                yield return n;
            }
        }

        internal static int ParseInt(string intStr, int fallback) => int.TryParse(intStr, out var n) ? n : fallback;

        internal static void Panic(string msg = "") {
            Console.Error.WriteLine(msg);
            Environment.Exit(1);
        }

        internal static void Assert(bool condition, string elsePanicWith = "") {
            if (!condition) {
                Panic(elsePanicWith);
            }
        }

    }
}
