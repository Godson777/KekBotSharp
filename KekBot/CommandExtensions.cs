using System.Linq;
using KekBot.Attributes;

namespace KekBot
{
    public static class CommandExtensions {

        public static Category GetCategory(this DSharpPlus.CommandsNext.Command command) =>
            command?.CustomAttributes.OfType<CategoryAttribute>().FirstOrDefault()?.category ?? Category.Uncategorized;

    }
}
