using System.Linq;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using KekBot.Attributes;

namespace KekBot {
    public static class CommandExtensions {

        public static Category GetCategory(this Command cmd) =>
            cmd?.CustomAttributes.OfType<CategoryAttribute>().FirstOrDefault()?.Category ?? Category.Uncategorized;

        public static bool IsCustomRequired(this CommandArgument arg) =>
            arg?.CustomAttributes.OfType<RequiredAttribute>().Any() ?? false;

        public static bool IsHidden(this CommandArgument arg) =>
            arg?.CustomAttributes.OfType<HiddenParam>().Any() ?? false;

        public static string? GetExtendedDescription(this Command cmd) => cmd?.CustomAttributes.OfType<ExtendedDescriptionAttribute>().FirstOrDefault()?.ExtendedDescription;
        
        /// <summary>
        /// Delays the reply to the user.
        /// After doing so, you can reply within 15 minutes using <see cref="InteractionContext.FollowUpAsync(DiscordFollowupMessageBuilder)"/>
        /// </summary>
        /// <param name="ctx">The interaction context</param>
        /// <returns></returns>
        internal static async Task SendThinking(this InteractionContext ctx) =>
            await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);

        /// <summary>
        /// Sends a response to the command recieved. For messages that consist of a basic string, see <seealso cref="ReplyBasicAsync(InteractionContext, string, bool)"/>
        /// </summary>
        /// <param name="ctx">The interaction context</param>
        /// <param name="builder">The interaction response builder</param>
        /// <returns></returns>
        internal static async Task ReplyAsync(this InteractionContext ctx, DiscordInteractionResponseBuilder builder) =>
            await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, builder);

        /// <summary>
        /// Sends a response to the command recieved.
        /// </summary>
        /// <param name="ctx">The interaction context</param>
        /// <param name="content">The content of the response</param>
        /// <param name="ephemeral">Whether or not the response is ephemeral</param>
        /// <returns></returns>
        internal static async Task ReplyBasicAsync(this InteractionContext ctx, string content, bool ephemeral = false) =>
            await ctx.ReplyAsync(new DiscordInteractionResponseBuilder().WithContent(content).AsEphemeral(ephemeral));

        /// <summary>
        /// Edits the interaction response.
        /// </summary>
        /// <param name="ctx">The interaction context</param>
        /// <param name="content">The new content of the response</param>
        internal static async Task EditBasicAsync(this InteractionContext ctx, string content) =>
            await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent(content));
    }
}
