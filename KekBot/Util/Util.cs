﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DSharpPlus.Entities;

namespace KekBot.Utils {
    static class Util {

        internal static readonly Random Rng = new Random();

        internal static T RandomElement<T>(this IEnumerable<T> list) => list.ElementAt(Rng.Next(list.Count()));

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

        internal static T? ToNullableClass<T>(this Optional<T> opt)
            where T : class => opt.HasValue ? opt.Value : null;

        internal static T? ToNullable<T>(this Optional<T> opt)
            where T : struct => opt.HasValue ? (T?)opt.Value : null;

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