using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;

namespace KekBot.Command.Commands {
    class WeebCommands : BaseCommandModule {

        [Command("awoo")]
        internal async Task Awoo(CommandContext ctx) {
            await ctx.RespondAsync("Awooo");
        }

    }
}
