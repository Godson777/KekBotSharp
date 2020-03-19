using System.Collections.Generic;
using System.Linq;
using KekBot.Utils;

namespace KekBot.Lib {
    internal class CommandInfoList : List<ICommandInfo> {
        public ICommandInfo GetFor(DSharpPlus.CommandsNext.Command cmd) =>
            GetFor(cmd.QualifiedName.Substring(0, cmd.QualifiedName.IndexOf(' ', System.StringComparison.Ordinal).FoundIndexOr(cmd.QualifiedName.Length)));

        public ICommandInfo GetFor(string name) =>
            this.First(cmdInfo => cmdInfo.Name.Equals(name, System.StringComparison.OrdinalIgnoreCase));
    }

    internal class FakeCommandsDictionary : Dictionary<string, IHasFakeCommands> {
        /// <summary>
        /// Returns true if the fake command "exists".
        /// </summary>
        public bool Contains(string name) =>
            ContainsKey(name) || Keys.Any(key => key.Equals(name, System.StringComparison.OrdinalIgnoreCase));

        public IHasFakeCommands? GetFaker(string name) =>
            TryGetValue(name, out var faker)
                ? faker
                : this.FirstOrDefault(pair => pair.Key.Equals(name, System.StringComparison.OrdinalIgnoreCase)).Value;
    }
}
