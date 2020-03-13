using System;
using System.Collections.Immutable;
using DSharpPlus.CommandsNext;
using KekBot.Attributes;

namespace KekBot.Utils {
    internal interface ICommandInfo : IEquatable<ICommandInfo> {
        string Name { get; }
        ImmutableArray<string> Aliases { get; }
        string Description { get; }
        Category Category { get; }
        ImmutableArray<ICommandOverloadInfo> Overloads { get; }
        DSharpPlus.CommandsNext.Command? Cmd { get; }
    }

    internal interface ICommandOverloadInfo {
        ImmutableArray<ICommandArgumentInfo> Arguments { get; }
        int Priority { get; }
    }

    interface ICommandArgumentInfo {
        public string Name { get; }
        public string Description { get; }
        public bool IsOptional { get; }
        public bool IsCustomRequired();
        public bool IsHidden();
    }
}