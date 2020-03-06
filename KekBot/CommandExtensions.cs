using System.Linq;
using Cmd = DSharpPlus.CommandsNext.Command;
using KekBot.Attributes;

namespace KekBot {
    public static class CommandExtensions {

        public static Category GetCategory(this Cmd command) =>
            command?.CustomAttributes.OfType<CategoryAttribute>().FirstOrDefault()?.Category ?? Category.Uncategorized;

    }
}
