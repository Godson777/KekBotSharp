using System.Collections.Generic;
using System.Linq;

namespace KekBot.Util
{
    static class Util
    {

        public static readonly System.Random Rng = new System.Random();

        public static T RandomElement<T>(this IEnumerable<T> list) => list.ElementAt(Rng.Next(list.Count()));

    }
}
