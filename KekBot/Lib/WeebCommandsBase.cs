using System;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using KekBot.Arguments;
using KekBot.Utils;
using Weeb.net;
using Weeb.net.Data;

namespace KekBot.Lib
{
    public class WeebCommandsBase
    {
        public WeebClient Client { get; }
        /// <summary>
        /// Await this task to wait for this to be ready to handle weeb.sh requests.
        /// </summary>
        public Task Initialized { get; }
        
        private const string EmbedFooter = "Powered by Weeb.sh!";
        private const string FailureMsg = "Failed to retrieve image";
        private const string FailureMsgNsfw = "This type probably has no NSFW images.";
        private const string FailureMsgSearch = "There were probably no results for your search.";

        public WeebCommandsBase(string botName, string botVersion, string weebToken)
        {
            Client = new WeebClient(botName, botVersion);
            Initialized = Client.Authenticate(weebToken, TokenType.Wolke);
        }
        
        public async Task FetchAndPost(
            InteractionContext ctx,
            string type,
            string msg,
            string flagsStr
        )
        {
            await ctx.SendThinking();

            var flags = FlagArgs.ParseString(flagsStr) ?? new FlagArgs();

            var requestTags = flags.Get("tags").NonEmpty()?.Split(',') ?? Array.Empty<string>();
            var requestHidden = flags.ParseBool("hidden") ?? false;
            var requestNsfw = ctx.Channel.IsNSFW
                ? (flags.ParseEnum<NsfwSearch>("nsfw") ?? NsfwSearch.True)
                : NsfwSearch.False;

            var builder = new DiscordEmbedBuilder();
            RandomData? image = await Client.GetRandomAsync(
                type: type,
                tags: requestTags,
                //fileType: FileType.Any,
                hidden: requestHidden,
                nsfw: requestNsfw
            );
            if (image == null) {
                builder.WithTitle(FailureMsg);

                if (requestTags.Length > 0 || requestHidden) {
                    builder.WithDescription(FailureMsgSearch);
                } else if (requestNsfw == NsfwSearch.Only) {
                    builder.WithDescription(FailureMsgNsfw);
                }
            } else {
                if (!ctx.Channel.IsNSFW && image.Nsfw)
                {
                    await ctx.ReplyBasicAsync(
                        "For some reason Weeb.sh gave me a NSFW image, but this is a SFW channel!");
                    return;
                }

                builder.WithTitle(msg).WithImageUrl(new Uri(image.Url));

                if (flags.ParseBool("debug") ?? false) {
                    var tagStr = Util.Join(image.Tags, tag => {
                        var extraInfo = string.Join("; ", new string[] {
                            tag.Hidden ? "hidden" : "",
                            string.IsNullOrEmpty(tag.User) ? "" : $"user {tag.User}",
                        }.Where(s => s.Length > 0));
                        return tag.Name + (extraInfo.Length > 0
                            ? $" ({extraInfo})"
                            : "");
                    });
                    builder.AddField("Tags", tagStr, inline: true);

                    var nsfwStr = image.Nsfw
                        ? "yes"
                        : (ctx.Channel.IsNSFW ? "no" : "not allowed in SFW channels");
                    builder.AddField("NSFW", nsfwStr, inline: true);

                    builder.AddField("Hidden", image.Hidden ? "yes" : "no", inline: true);
                }
            }
            builder.WithFooter(EmbedFooter);
            
            await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(builder.Build()));
        }
        
        public async Task FetchAndPostWithMention(
            InteractionContext ctx,
            string type,
            string msg,
            DiscordMember user,
            string flagsStr
        ) => await FetchAndPost(
            ctx,
            type: type, 
            msg: string.Format(msg, user.DisplayName, ctx.Interaction.User.GetName()),
            flagsStr: flagsStr
        );
    }
}