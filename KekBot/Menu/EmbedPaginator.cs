using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KekBot.Menu {
    public class EmbedPaginator : Menu {

        public DiscordColor Color { get; set; }
        public Action<DiscordMessage>? FinalAction { get; set; } = async m => await m.DeleteAllReactionsAsync();
        public bool ShowPageNumbers { get; set; }
        protected int Pages => Embeds.Count;
        public List<DiscordEmbed> Embeds { get; private set; } = new List<DiscordEmbed>();

        protected string Left { get; private set; } = "◀️";
        protected string Stop { get; private set; } = "⏹️";
        protected string Right { get; private set; } = "▶️";
        protected string[] LeftRightStop() => new string[] { Left, Right, Stop };
        public EmbedPaginator(InteractivityExtension interactivity) : base(interactivity) {
        }

        public override async Task Display(DiscordChannel channel) {
            //TODO: Implement permission checks
            await Paginate(channel, 1);
        }

        public override async Task Display(DiscordMessage message) {
            await Paginate(message, 1);
        }

        private async Task Paginate(DiscordChannel channel, int pageNum) {
            if (pageNum < 1)
                pageNum = 1;
            else if (pageNum > Pages)
                pageNum = Pages;
            var msg = RenderPage(pageNum);
            await Initialize(await channel.SendMessageAsync(embed: msg), pageNum);
        }

        private async Task Paginate(DiscordMessage message, int pageNum) {
            if (pageNum < 1)
                pageNum = 1;
            else if (pageNum > Pages)
                pageNum = Pages;
            var msg = RenderPage(pageNum);
            await Initialize(await message.ModifyAsync(embed: msg), pageNum);
        }

        private async Task Initialize(DiscordMessage message, int pageNum) {
            if (Pages > 1) {
                await message.CreateReactionAsync(DiscordEmoji.FromUnicode(Left));
                await message.CreateReactionAsync(DiscordEmoji.FromUnicode(Stop));
                await message.CreateReactionAsync(DiscordEmoji.FromUnicode(Right));
            } else {
                await message.CreateReactionAsync(DiscordEmoji.FromUnicode(Stop));
            }
            await Pagination(message, pageNum);
        }

        private async Task Pagination(DiscordMessage message, int pageNum) {
            var result = await Interactivity.WaitForReactionAsync(react => {
                if (react.Message.Id != message.Id) return false;
                if (!LeftRightStop().Contains(react.Emoji.Name)) return false;
                return IsValidUser(react.User, react.Guild);
            }, Timeout);

            if (result.TimedOut) {
                FinalAction?.Invoke(message);
                return;
            }

            var newPageNum = pageNum;
            var e = result.Result.Emoji.Name;
            if (e == Left) {
                if (newPageNum > 1) newPageNum--;
            } else if (e == Right) {
                if (newPageNum < Pages) newPageNum++;
            } else if (e == Stop) {
                FinalAction?.Invoke(message);
                return;
            }

            await result.Result.Message.DeleteReactionAsync(result.Result.Emoji, result.Result.User);
            var m = await message.ModifyAsync(embed: RenderPage(newPageNum));
            await Pagination(m, newPageNum);
        }

        private DiscordEmbed RenderPage(int pageNum) {
            var builder = new DiscordEmbedBuilder();
            var e = Embeds[pageNum - 1];

            foreach (var field in e.Fields) {
                builder.AddField(field.Name, field.Value, field.Inline);
            }
            builder.Title = e.Title ?? "";
            builder.Description = e.Description;
            builder.Color = (DiscordColor?)e.Color ?? Color;
            builder.ThumbnailUrl = e.Thumbnail?.Url?.ToString();
            builder.Timestamp = e.Timestamp;
            builder.WithAuthor(e.Author?.Name, e.Author?.Url?.ToString(), e.Author?.IconUrl?.ToString());

            if (ShowPageNumbers) builder.WithFooter($"Page {pageNum}/{Pages}" + (e.Footer != null ? $" | {e.Footer.Text}" : ""));
            else builder.WithFooter(e.Footer.Text);

            return builder.Build();
        }
    }
}
