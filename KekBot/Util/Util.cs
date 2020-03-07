using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace KekBot.Utils {
    static class Util {

        internal static readonly System.Random Rng = new System.Random();

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

        //internal static IEnumerable<T> StepBy<T>(this IEnumerable<T> list, int step) {
        //    if (step == 0) throw new System.ArgumentOutOfRangeException(paramName: nameof(step));

        //    int i = step;
        //    foreach (var item in list) {
        //        if (i == step) yield return item;
        //        i = i == 0 ? step : i - 1;
        //    }
        //}

        internal static IEnumerable<int> Range(int start = 0, int end = int.MaxValue, int step = 1) {
            for (int n = start; n < end; n += step) {
                yield return n;
            }
        }

    }
}
