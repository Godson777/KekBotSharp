using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;

namespace KekBot.Utils {
    /// <summary>
    /// Class for getting random numbers and managing instances of itself, thread-locally.
    /// </summary>
    class Randumb : Random {

        /// <summary>
        /// Only used to instantiate new Random objects per thread, to ensure no two threads are the same.
        /// Don't use this without locking it. It's not thread-safe.
        /// </summary>
        protected static readonly Random GlobalRng = new Random();

        protected static readonly ThreadLocal<Randumb> ThreadRng = new ThreadLocal<Randumb>(() => {
            lock (GlobalRng) {
                return new Randumb(GlobalRng.Next());
            }
        });

        /// <summary>
        /// Makes a new Randumb with the given seed. You probably don't want to use this directly.
        /// </summary>
        public Randumb(int Seed) : base(Seed) { }

        /// <summary>
        /// Get a thread-local instance of this class.
        /// </summary>
        public static Randumb Instance { get => ThreadRng.Value!; }

        /// <summary>
        /// Returns one of the given elements.
        /// </summary>
        [SuppressMessage("Globalization", "CA1303:Do not pass literals as localized parameters", Justification = "Shouldn't appear")]
        public T OneOf<T>(params T[] list) => list.Length == 0
            ? throw new ArgumentOutOfRangeException(paramName: nameof(list), message: "Cannot choose from nothing!")
            : list[Next(list.Length)];

        /// <summary>
        /// Returns one of the elements of the given enumerable.
        /// </summary>
        [SuppressMessage("Globalization", "CA1303:Do not pass literals as localized parameters", Justification = "Shouldn't appear")]
        public T OneFrom<T>(IEnumerable<T> list) => list.Any()
            ? list.ElementAt(Next(list.Count()))
            : throw new ArgumentOutOfRangeException(paramName: nameof(list), message: "Cannot choose from nothing!");

    }
}
