﻿using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using System.Threading.Tasks;

namespace KekBot {
    public class PingCommand : BaseCommandModule {

        [Command("ping"), Description("Returns with the bot's ping."), Aliases("pong")]
        async Task Ping(CommandContext ctx) {
            var msg = await ctx.RespondAsync("Pinging...");
            var ping = msg.Timestamp - ctx.Message.Timestamp;
            var heartbeat = ctx.Client.Ping;
            await msg.ModifyAsync(
                $"🏓 Pong! `{ping.TotalMilliseconds}ms`\n" +
                $"💓 Heartbeat: `{heartbeat}ms`"
            );
        }

    }
}