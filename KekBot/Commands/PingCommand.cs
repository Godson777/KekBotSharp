using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using System.Threading.Tasks;

namespace KekBot.Commands {
    public class PingCommand : ApplicationCommandModule {

        [SlashCommand("ping", "Returns with the bot's ping.")]
        async Task Ping(InteractionContext ctx)
        {
            await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                new DiscordInteractionResponseBuilder().WithContent("Pinging..."));
            var msg = await ctx.GetOriginalResponseAsync();
            var ping = msg.CreationTimestamp - ctx.Interaction.CreationTimestamp;
            await msg.ModifyAsync(
                $"🏓 Pong! `{ping.TotalMilliseconds}ms`\n" +
                $"💓 Heartbeat: `{ctx.Client.Ping}ms`"
            );
        }

    }
}
