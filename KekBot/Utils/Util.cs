using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using KekBot.Commands;
using KekBot.Lib;

namespace KekBot.Utils {
    static class Util {

        /// <summary>
        /// Returns a random element from the collection.
        /// </summary>
        internal static T RandomElement<T>(this IEnumerable<T> coll, Randumb? rng = null) =>
            (rng ?? Randumb.Instance).OneFrom(coll);

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

        /// <summary>
        /// Tries to parse the string as a Uri.
        /// </summary>
        /// <param name="s">String to parse.</param>
        /// <param name="uri">Parsed Uri value on success. Null on failure.</param>
        /// <returns>Whether the conversion succeeded.</returns>
        internal static bool TryParseUri(string? s, [MaybeNullWhen(false)] out Uri uri)
        {
            try
            {
                uri = new Uri(s!); // it throws on null
                return true;
            }
            catch (ArgumentNullException)
            {
            }
            catch (FormatException)
            {
            }

            uri = null;
            return false;
        }

        /// <summary>
        /// Wait for the tasks in parallel. If any fail, throws the first exception.
        /// </summary>
        /// <remarks>
        /// If you need more/less than 3, add an overload. I wish C# had this build in.
        /// </remarks>
        internal static async Task<(T, T, T)> AllTasks<T>(Task<T> t1, Task<T> t2, Task<T> t3)
        {
            T[] a;
            try
            {
                a = await Task.WhenAll(t1, t2, t3);
            }
            catch (AggregateException e)
            {
                throw e.InnerExceptions.First();
            }
            return (a[0], a[1], a[2]);
        }

        internal static string GetName(this DiscordUser user) =>
            user is DiscordMember m ? m.DisplayName : user.Username;

        internal static string AuthorName(this DiscordMessage msg) => msg.Author.GetName();

        internal static string GetRawArgString(this CommandContext ctx, string cmdName) =>
            ctx.Message.GetRawArgString(ctx.Prefix, cmdName);

        internal static string GetRawArgString(this DiscordMessage msg, string prefix, string cmdName) {
            var content = msg.Content;
            var afterPrefix = content.FastIndexOfEnd(prefix).FoundIndexOr(0);
            var afterCmd = content.FastIndexOfEnd(cmdName, afterPrefix).FoundIndexOr(0);
            return content.Substring(afterCmd).Trim();
        }

        internal static IEnumerable<int> Range(int start = 0, int end = int.MaxValue, int step = 1) {
            for (int n = start; n < end; n += step) {
                yield return n;
            }
        }

        internal static int ParseInt(string intStr, int fallback) => int.TryParse(intStr, out var n) ? n : fallback;

        /// <summary>
        /// Joins the enumerable if it's non-null and nonempty or returns a fallback string.
        /// </summary>
        internal static string Join<T>(IEnumerable<T>? coll, Func<T, string>? stringifier = null,
            string sep = ", ", string fallback = "none") =>
            (coll == null || !coll.Any())
                ? fallback
                : string.Join(sep, coll.Select(stringifier ?? (x => x?.ToString() ?? "null")));


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

        public static void Shuffle<T>(this IList<T> list, Randumb? rng = null) {
            rng ??= Randumb.Instance;
            int n = list.Count;
            while (n > 1) {
                n--;
                int k = rng.Next(n + 1);
                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }

        public static String PrintTimeSpan(TimeSpan time) {
            return $"{(time.Days > 0 ? $"{time.Days} Days, " : "")}{(time.Hours > 0 ? $"{time.Hours} Hours, " : "")}{time.Minutes} Minute{(time.Minutes > 1 ? "s" : "")}, and {time.Seconds} Second{(time.Seconds > 1 ? "s" : "")}.";
        }

        public static string ToDurationString(this TimeSpan ts) {
            if (ts.TotalHours >= 1)
                return ts.ToString(@"h\:mm\:ss");
            return ts.ToString(@"m\:ss");
        }

        /// <summary>
        /// Converts given <see cref="MusicItem"/> to a track string.
        /// </summary>
        /// <param name="mi">Music item to convert.</param>
        /// <returns>Track string.</returns>
        public static string ToTrackString(this MusicItem x) {
            return $"{Formatter.InlineCode(x.Track.Title)} by {Formatter.InlineCode(x.Track.Author)} [{x.Track.Length.ToDurationString()}] (added by {x.RequestedBy.DisplayName})";
        }

        public static bool IsImage(this DiscordAttachment a) {
            return a.Width > 0 && a.Height > 0;
        }

        private static readonly HttpClient HttpClient = new HttpClient();

        /// <summary>
        /// Checks if the given address exists.
        /// </summary>
        /// <remarks>
        /// Sends a HEAD request. This is faster than GET since it doesn't have
        /// to download the body of the page.
        /// </remarks>
        /// <returns>Whether the web page exists.</returns>
        private static async Task<bool> WebPageExists(string address)
        {
            using var request = new HttpRequestMessage(HttpMethod.Head, address);
            return (await HttpClient.SendAsync(request)).IsSuccessStatusCode;
        }

        /// <summary>
        /// Gets the member's avatar URL.
        /// </summary>
        /// <param name="m">The member</param>
        /// <param name="preference">Which avatar to get</param>
        /// <returns>The URL (it's probably valid to turn into a <see cref="Uri"/>)</returns>
        public static string AvatarUrl(this DiscordMember m,
            AvatarPreference preference) =>
            preference == AvatarPreference.Guild
                ? m.GuildAvatarUrl
                : m.AvatarUrl;

        /// <summary>
        /// Gets the member's avatar URL. If <paramref name="preference"/> is guild, checks whether
        /// they have one (with a web request), and if not, returns their global one instead.
        /// </summary>
        /// <param name="m">The member</param>
        /// <param name="preference">Which avatar to get</param>
        /// <returns>The URL (it's probably valid to turn into a <see cref="Uri"/>)</returns>
        public static async Task<string> AvatarUrlChecked(this DiscordMember m,
            AvatarPreference preference) =>
            preference == AvatarPreference.Guild && await WebPageExists(m.GuildAvatarUrl)
                ? m.GuildAvatarUrl
                : m.AvatarUrl;

        /// <summary>
        /// Opens a readable stream containing the member's avatar.
        /// </summary>
        /// <returns>A stream or null if it couldn't load</returns>
        public static async Task<Stream?> OpenReadAvatarAsync(this DiscordMember m,
            AvatarPreference preference,
            WebClient? withClient = null)
        {
            using var client = withClient ?? new WebClient();

            return
                await client.OpenReadTaskAsync(m.AvatarUrl(preference))
                    .Ext().Catch<WebException>() ??
                // Server avatars seem a little wonky, so try global before giving up.
                await client.OpenReadTaskAsync(m.AvatarUrl)
                    .Ext().Catch<WebException>();
        }

        /// <summary>
        /// Wrap in a type with useful methods. See <see cref="TaskExt{TResult}"/>
        /// </summary>
        /// <param name="task">The task to use</param>
        /// <typeparam name="TResult">The success type</typeparam>
        /// <returns>A temporary value</returns>
        internal static TaskExt<TResult> Ext<TResult>(this Task<TResult> task)
            where TResult : class =>
            new TaskExt<TResult>(task);
    }

    /// <summary>
    /// A wrapper around a <see cref="Task{TResult}"/> that provides useful methods.
    /// </summary>
    /// <remarks>
    /// They <em>could</em> be extension methods, but if you specify a type param,
    /// you have to specify <em>every</em> type param, which is annoying.
    /// </remarks>
    /// <typeparam name="TResult">The success type</typeparam>
    internal readonly struct TaskExt<TResult> where TResult : class
    {
        private readonly Task<TResult> Task;

        public TaskExt(Task<TResult> task) => Task = task;

        /// <summary>
        /// On failure, catch the given exception and return null.
        /// </summary>
        /// <typeparam name="TException">The exception to catch</typeparam>
        /// <returns>The success value or null</returns>
        public async Task<TResult?> Catch<TException>()
        {
            try
            {
                return await Task;
            }
            catch (Exception e) when (e is TException)
            {
                return null;
            }
        }

        /// <summary>
        /// Ignore the task's result and get whether it succeeds.
        /// </summary>
        /// <returns>True on success, false on failure</returns>
        public async Task<bool> Succeeds()
        {
            try
            {
                await Task;
                return true;
            }
#pragma warning disable CA1031
            catch
#pragma warning restore CA1031
            {
                return false;
            }
        }
    }
}
