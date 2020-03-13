using System.Linq;
using DSharpPlus.CommandsNext;
using KekBot.Attributes;

namespace KekBot {
    public static class CommandExtensions {

        public static Category GetCategory(this Command cmd) =>
            cmd?.CustomAttributes.OfType<CategoryAttribute>().FirstOrDefault()?.Category ?? Category.Uncategorized;

        public static bool IsCustomRequired(this CommandArgument arg) =>
            arg?.CustomAttributes.OfType<RequiredAttribute>().Any() ?? false;

    }
}
