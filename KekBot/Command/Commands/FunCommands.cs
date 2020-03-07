using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using KekBot.Attributes;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace KekBot.Command.Commands {
    class FunCommands : BaseCommandModule {
        [Command("8ball"), Description("Ask the magic 8-ball a question!"), Category(Category.Fun)]
        public async Task eightball(CommandContext ctx, [RemainingText, Description("The question to ask to the magic 8-ball.")] string question) {
            if (question == "") {
                
            }
        }

    }
}
