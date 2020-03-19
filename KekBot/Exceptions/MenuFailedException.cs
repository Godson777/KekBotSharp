using System;
using System.Runtime.Serialization;

namespace KekBot.Menu {
    [Serializable]
    internal class MenuFailedException : Exception {
        public MenuFailedException() : base("The menu failed to display due to failing checks.") {
        }

        public MenuFailedException(string? message) : base($"The menu failed to display due to a failing check. Message: {message}") {
        }

        public MenuFailedException(string? message, Exception? innerException) : base(message, innerException) {
        }
    }
}