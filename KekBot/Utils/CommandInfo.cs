using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using DSharpPlus.CommandsNext;
using KekBot.Attributes;
using Cmd = DSharpPlus.CommandsNext.Command;

namespace KekBot.Utils {
    internal struct CommandInfo : ICommandInfo {
        public string Name { get; }
        public ImmutableArray<string> Aliases { get; }
        public string Description { get; }
        public Category Category { get; }
        public ImmutableArray<ICommandOverloadInfo> Overloads { get; }
        public Cmd? Cmd { get; }


        public CommandInfo(Cmd cmd) {
            Cmd = cmd;
            Name = cmd.Name;
            Aliases = cmd.Aliases.ToImmutableArray();
            Description = cmd.Description;
            Category = cmd.GetCategory();
            Overloads = cmd.Overloads.Select(ovrld => (ICommandOverloadInfo)new CommandOverloadInfo(ovrld)).ToImmutableArray();
            Util.Assert(Overloads != null, elsePanicWith: "wtf, why did ToImmutableArray return null");
        }

        public bool Equals([AllowNull] ICommandInfo other) => Name == other.Name;
    }

    internal struct CommandOverloadInfo : ICommandOverloadInfo {
        public ImmutableArray<ICommandArgumentInfo> Arguments { get; }
        public int Priority { get; }

        public CommandOverloadInfo(CommandOverload ovrld) {
            Arguments = ovrld.Arguments.Select(arg => (ICommandArgumentInfo)new CommandArgumentInfo(arg)).ToImmutableArray();
            Priority = ovrld.Priority;
        }
    }

    internal struct CommandArgumentInfo : ICommandArgumentInfo {
        public string Name { get; }
        public string Description { get; }
        public bool IsOptional { get; }
        public bool IsCustomRequired() => _IsCustomRequired;
        private readonly bool _IsCustomRequired;
        public bool IsHidden() => _IsHidden;
        private readonly bool _IsHidden;


        public CommandArgumentInfo(CommandArgument arg) {
            Name = arg.Name;
            Description = arg.Description;
            IsOptional = arg.IsOptional;
            _IsCustomRequired = arg.IsCustomRequired();
            _IsHidden = arg.IsHidden();
        }
    }
}
