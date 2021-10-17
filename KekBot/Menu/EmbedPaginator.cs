using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using KekBot.Utils;

namespace KekBot.Menu {
    public class EmbedPaginator : Menu {

        /// <summary>
        /// The color the Embeds will use.
        /// </summary>
        public DiscordColor Color { get; set; } = DiscordColor.White;
        /// <summary>
        /// This final action is called when the user hits the stop button. (Or the paginator times out from no response.)
        /// By default, the final action will disable the buttons embedded in the message.
        /// </summary>
        public Action<DiscordMessage>? FinalAction { private get; set; } = async m => {
            var buttons = m.Components.ToList()[0].Components.ToList();
            var disabledBtns = new List<DiscordButtonComponent>();

            foreach (DiscordButtonComponent btn in buttons) {
                btn.Disabled = true;
                disabledBtns.Add(btn);
            }

            await m.ModifyAsync(new DiscordMessageBuilder()
                .WithContent(m.Content)
                .AddEmbeds(m.Embeds)
                .AddComponents(disabledBtns.ToArray()));
        };
        /// <summary>
        /// If true, the page count will be shown at the footer.
        /// </summary>
        public bool ShowPageNumbers { get; set; } = true;
        protected int Pages => Embeds.Count;
        /// <summary>
        /// The list of embeds to paginate.
        /// </summary>
        public List<DiscordEmbed> Embeds { get; private set; } = new List<DiscordEmbed>();
        public bool WrapPageEnds { private get; set; } = true;

        protected DiscordButtonComponent LeftBtn { get; private set; } = new DiscordButtonComponent(DSharpPlus.ButtonStyle.Primary, "paginator_left", null, emoji: new DiscordComponentEmoji("◀️"));
        protected DiscordButtonComponent StopBtn { get; private set; } = new DiscordButtonComponent(DSharpPlus.ButtonStyle.Primary, "paginator_stop", null, emoji: new DiscordComponentEmoji("⏹"));
        protected DiscordButtonComponent RightBtn { get; private set; } = new DiscordButtonComponent(DSharpPlus.ButtonStyle.Primary, "paginator_right", null, emoji: new DiscordComponentEmoji("▶"));

        public EmbedPaginator(InteractivityExtension interactivity) : base(interactivity) {
        }

        public override async Task Display(DiscordChannel channel) {
            DisplayChecks();
            await Paginate(channel, 1);
        }

        public override async Task Display(DiscordMessage message) {
            DisplayChecks();
            await Paginate(message, 1);
        }

        private protected override void DisplayChecks() {
            if (Users.Count == 0 && Roles.Count == 0) throw new MenuFailedException();
            if (Embeds.Count == 0) throw new MenuFailedException();
        }

        private async Task Paginate(DiscordChannel channel, int pageNum) {
            if (pageNum < 1)
                pageNum = 1;
            else if (pageNum > Pages)
                pageNum = Pages;
            var msg = RenderPage(pageNum);
            await Pagination(await channel.SendMessageAsync(new DiscordMessageBuilder()
                .WithEmbed(msg)
                .AddComponents(GetButtonList())), pageNum);
        }

        private async Task Paginate(DiscordMessage message, int pageNum) {
            if (pageNum < 1)
                pageNum = 1;
            else if (pageNum > Pages)
                pageNum = Pages;
            var msg = RenderPage(pageNum);
            await Pagination(await message.ModifyAsync(new DiscordMessageBuilder()
                .WithEmbed(msg)
                .AddComponents(GetButtonList())), pageNum);
        }

        private DiscordComponent[] GetButtonList() {
            var buttons = new List<DiscordComponent>();
            if (Pages > 1) {
               buttons.Add(LeftBtn);
                buttons.Add(StopBtn);
                buttons.Add(RightBtn);
            } else {
                buttons.Add(StopBtn);
            }
            return buttons.ToArray();
        }

        private async Task Pagination(DiscordMessage message, int pageNum) {
            var result = await Interactivity.WaitForButtonAsync(message, Timeout);

            if (result.TimedOut) {
                FinalAction?.Invoke(message);
                return;
            }

            //Throw an error as an emphemeral message if the user attempting to interact isn't "valid".
            if (!IsValidUser(result.Result.Interaction.User, result.Result.Interaction.Guild)) {
                await result.Result.Interaction.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent("Only the person who ran the command can interact with this menu.").AsEphemeral(true));
                await Pagination(result.Result.Message, pageNum);
                return;
            }

            var newPageNum = pageNum;
            var e = result.Result.Id;
            if (e == LeftBtn.CustomId) {
                if (newPageNum == 1 && WrapPageEnds) newPageNum = Pages + 1;
                if (newPageNum > 1) newPageNum--;
            } else if (e == RightBtn.CustomId) {
                if (newPageNum == Pages && WrapPageEnds) newPageNum = 0;
                if (newPageNum < Pages) newPageNum++;
            } else if (e == StopBtn.CustomId) {
                await result.Result.Interaction.CreateResponseAsync(InteractionResponseType.DeferredMessageUpdate);
                FinalAction?.Invoke(result.Result.Message);
                return;
            }


            await result.Result.Interaction.CreateResponseAsync(InteractionResponseType.UpdateMessage, new DiscordInteractionResponseBuilder()
                .AddEmbed(RenderPage(newPageNum))
                .AddComponents(GetButtonList()));
            await Pagination(result.Result.Message, newPageNum);
        }

        private DiscordEmbed RenderPage(int pageNum) {
            var builder = new DiscordEmbedBuilder();
            var e = Embeds[pageNum - 1];

            foreach (var field in e.Fields) {
                builder.AddField(field.Name, field.Value, field.Inline);
            }
            builder.Title = e.Title ?? "";
            builder.Description = e.Description;
            builder.Color = e.Color.ToNullable() ?? Color;
            builder.WithThumbnail(e.Thumbnail?.Url?.ToString());
            builder.Timestamp = e.Timestamp;
            builder.WithAuthor(e.Author?.Name, e.Author?.Url?.ToString(), e.Author?.IconUrl?.ToString());

            if (ShowPageNumbers) builder.WithFooter($"Page {pageNum}/{Pages}" + (e.Footer != null ? $" | {e.Footer.Text}" : ""));
            else builder.WithFooter(e.Footer.Text);

            return builder.Build();
        }
    }
}
