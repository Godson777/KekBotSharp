using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace KekBot.Menu {
    public class EmbedPaginator : Menu {

        public DiscordColor color { private get; set; }
        public Action<DiscordMessage>? finalAction { private get; set; }
        public bool showPageNumbers { private get; set; }
        protected int pages {
            get {
                return embeds.Count;
            }
        }
        public List<DiscordEmbed> embeds { get; private set; } = new List<DiscordEmbed>();

        protected string LEFT { get; private set; } = "◀️";
        protected string STOP { get; private set; } = "⏹️";
        protected string RIGHT { get; private set; } = "▶️";
        public EmbedPaginator(InteractivityExtension interactivity) : base(interactivity) {
        }

        public override async Task Display(DiscordChannel channel) {
            await Paginate(channel, 1);
        }

        public override async Task Display(DiscordMessage message) {
            await Paginate(message, 1);
        }

        private async Task Paginate(DiscordChannel channel, int pageNum) {
            if (pageNum < 1)
                pageNum = 1;
            else if (pageNum > pages)
                pageNum = pages;
            var msg = RenderPage(pageNum);
            await Initialize(await channel.SendMessageAsync(embed: msg), pageNum);
        }

        private async Task Paginate(DiscordMessage message, int pageNum) {
            if (pageNum < 1)
                pageNum = 1;
            else if (pageNum > pages)
                pageNum = pages;
            var msg = RenderPage(pageNum);
            await Initialize(await message.ModifyAsync(embed: msg), pageNum);
        }

        private async Task Initialize(DiscordMessage message, int pageNum) {
            if (pages > 1) {
                await message.CreateReactionAsync(DiscordEmoji.FromUnicode(LEFT));
                await message.CreateReactionAsync(DiscordEmoji.FromUnicode(STOP));
                await message.CreateReactionAsync(DiscordEmoji.FromUnicode(RIGHT));
             } else {
                await message.CreateReactionAsync(DiscordEmoji.FromUnicode(STOP));
            }
            await Pagination(message, pageNum);
        }

        private async Task Pagination(DiscordMessage message, int pageNum) {
            var result = await interactivity.WaitForReactionAsync(react => {
                if (react.Message.Id != message.Id) return false;
                if (!(LEFT.Equals(react.Emoji.Name) || STOP.Equals(react.Emoji.Name) || RIGHT.Equals(react.Emoji.Name))) return false;
                return isValidUser(react.User, react.Guild);
            }, timeout);

            if (result.TimedOut) {
                finalAction.Invoke(message);
                return;
            }

            var newPageNum = pageNum;
            var e = result.Result.Emoji.Name;
            if (e.Equals(LEFT)) {
                if (newPageNum > 1) newPageNum--;
            }
            if (e.Equals(RIGHT)) {
                if (newPageNum < pages) newPageNum++;
            }
            if (e.Equals(STOP)) {
                finalAction.Invoke(message);
                return;
            }
            await result.Result.Message.DeleteReactionAsync(result.Result.Emoji, result.Result.User);
            var m = await message.ModifyAsync(embed: RenderPage(newPageNum));
            await Pagination(m, newPageNum);
        }

        private DiscordEmbed RenderPage(int pageNum) {
            DiscordEmbedBuilder builder = new DiscordEmbedBuilder();
            DiscordEmbed e = embeds[pageNum - 1];

            foreach (DiscordEmbedField field in e.Fields) {
                builder.AddField(field.Name, field.Value, field.Inline);
            }
            if (e.Title != null) builder.Title = e.Title;
            if (e.Description != null) builder.Description = e.Description;
            if (e.Color != null) builder.Color = e.Color;
            else builder.Color = color;
            if (e.Thumbnail != null) builder.ThumbnailUrl = e.Thumbnail.Url.ToString();
            if (e.Thumbnail != null) builder.Timestamp = e.Timestamp;
            if (e.Author != null) builder.WithAuthor(e.Author.Name != null ? e.Author.Name : null, 
                e.Author.Url != null ? e.Author.Url.ToString() : null, 
                e.Author.IconUrl != null ? e.Author.IconUrl.ToString() : null);

            if (showPageNumbers) builder.WithFooter($"Page {pageNum}/{pages}" + (e.Footer != null ? $" | {e.Footer.Text}" : ""));
            else builder.WithFooter(e.Footer.Text);
            return builder.Build();
        }
    }
}
