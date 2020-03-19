using System.Linq;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;

namespace KekBot.Lib {
    interface IHasFakeCommands {

        internal ICommandInfo[] FakeCommandInfo { get; }

        internal string[] FakeCommands { get => FakeCommandInfo.Select(cmdInfo => cmdInfo.Name).ToArray(); }

        internal Task HandleFakeCommand(CommandContext ctx, string cmdName);

    }
}
