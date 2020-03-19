using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using DSharpPlus.CommandsNext;
using KekBot.Attributes;

namespace KekBot.Lib {
    /// <summary>
    /// The ICommandInfo for real commands.
    /// </summary>
    internal struct CommandInfo : ICommandInfo {
        public string Name { get; }
        public ImmutableArray<string> Aliases { get; }
        public string Description { get; }
        public Category Category { get; }
        public ImmutableArray<ICommandOverloadInfo> Overloads { get; }
        public Command? Cmd { get; }

        public CommandInfo(Command cmd) {
            Cmd = cmd;
            Name = cmd.Name;
            Aliases = cmd.Aliases.ToImmutableArray();
            Description = cmd.Description;
            Category = cmd.GetCategory();
            Overloads = cmd.Overloads.Select(CommandOverloadInfo.From).ToImmutableArray();
        }

        public static ICommandInfo From(Command cmd) => new CommandInfo(cmd);

        public bool Equals([AllowNull] ICommandInfo other) => Name == other.Name;
    }

    /// <summary>
    /// The ICommandOverloadInfo for real commands' overloads.
    /// </summary>
    internal struct CommandOverloadInfo : ICommandOverloadInfo {
        public ImmutableArray<ICommandArgumentInfo> Arguments { get; }
        public int Priority { get; }

        public CommandOverloadInfo(CommandOverload ovrld) {
            Arguments = ovrld.Arguments.Select(CommandArgumentInfo.From).ToImmutableArray();
            Priority = ovrld.Priority;
        }

        public static ICommandOverloadInfo From(CommandOverload ovrld) => new CommandOverloadInfo(ovrld);
    }

    /// <summary>
    /// The ICommandArgumentInfo for real commands' arguments.
    /// </summary>
    internal struct CommandArgumentInfo : ICommandArgumentInfo {
        public string Name { get; }
        public string Description { get; }
        public bool IsOptional { get; }
        public bool IsHidden { get; }

        public CommandArgumentInfo(CommandArgument arg) {
            Name = arg.Name;
            Description = arg.Description;
            IsOptional = arg.IsOptional && !arg.IsCustomRequired();
            IsHidden = arg.IsHidden();
        }

        public static ICommandArgumentInfo From(CommandArgument arg) => new CommandArgumentInfo(arg);
    }
}
