using System.Linq;
using Cmd = DSharpPlus.CommandsNext.Command;
using Arg = DSharpPlus.CommandsNext.CommandArgument;
using KekBot.Attributes;
using DSharpPlus.CommandsNext;

namespace KekBot {
    public static class CommandExtensions {

        public static Category GetCategory(this Cmd command) =>
            command?.CustomAttributes.OfType<CategoryAttribute>().FirstOrDefault()?.Category ?? Category.Uncategorized;

        public static bool IsCustomRequired(this Arg argument) => argument?.CustomAttributes.OfType<RequiredAttribute>().Any() ?? false;

    }
}
