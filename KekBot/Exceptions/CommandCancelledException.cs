using System;
using System.Runtime.Serialization;

namespace KekBot.Commands {
    [Serializable]
    internal class CommandCancelledException : Exception {
        public CommandCancelledException() : base("Command execution was cancelled due to unmet criteria.") {}

        public CommandCancelledException(string? message) : base(message) {}

        public CommandCancelledException(Exception? innerException) : base("Command execution was cancelled due to unmet criteria.", innerException) { }

        public CommandCancelledException(string? message, Exception? innerException) : base(message, innerException) {}
    }
}