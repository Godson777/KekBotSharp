using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using System.Threading.Tasks;

namespace KekBot.Commands {
    public class PingCommand : ApplicationCommandModule {

        [SlashCommand("ping", "Returns with the bot's ping.")]
        async Task Ping(InteractionContext ctx)
        {
            await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);
            var msg = await ctx.GetOriginalResponseAsync();
            var ping = msg.CreationTimestamp - ctx.Interaction.CreationTimestamp;
            await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent(
                $"🏓 Pong! `{ping.TotalMilliseconds}ms`\n" +
                $"💓 Heartbeat: `{ctx.Client.Ping}ms`"
            ));
        }

    }
}
