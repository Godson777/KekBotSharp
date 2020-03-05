using DSharpPlus.CommandsNext;
using KekBot.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace KekBot {
    public static class CommandExtentions {
        public static Category GetCategory(this DSharpPlus.CommandsNext.Command command) {
            if (command.CustomAttributes.Any(a => a is CategoryAttribute)) return command.CustomAttributes.Where(c => c is CategoryAttribute).Cast<CategoryAttribute>().FirstOrDefault().category;
            else return Category.Uncategorized;
        }
    }
}
