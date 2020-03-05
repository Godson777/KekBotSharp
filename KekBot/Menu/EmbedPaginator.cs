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

        public DiscordColor Color { private get; set; }
        public Action<DiscordMessage>? FinalAction { private get; set; }
        public bool ShowPageNumbers { private get; set; }
        protected int Pages => Embeds.Count;
        public List<DiscordEmbed> Embeds { get; private set; } = new List<DiscordEmbed>();

        protected string LEFT { get; private set; } = "◀️";
        protected string STOP { get; private set; } = "⏹️";
        protected string RIGHT { get; private set; } = "▶️";
        protected string[] LEFT_RIGHT_STOP => new string[] { LEFT, RIGHT, STOP };
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
                if (!LEFT_RIGHT_STOP.Contains(react.Emoji.Name)) return false;
                return isValidUser(react.User, react.Guild);
            }, timeout);

            if (result.TimedOut) {
                FinalAction?.Invoke(message);
                return;
            }

            var newPageNum = pageNum;
            var e = result.Result.Emoji.Name;
            if (e == LEFT) {
                if (newPageNum > 1) newPageNum--;
            } else if (e == RIGHT) {
                if (newPageNum < Pages) newPageNum++;
            } else if (e == STOP) {
                FinalAction?.Invoke(message);
                return;
            }

            await result.Result.Message.DeleteReactionAsync(result.Result.Emoji, result.Result.User);
            var m = await message.ModifyAsync(embed: RenderPage(newPageNum));
            await Pagination(m, newPageNum);
        }

        private DiscordEmbed RenderPage(int pageNum) {
            DiscordEmbedBuilder builder = new DiscordEmbedBuilder();
            DiscordEmbed e = Embeds[pageNum - 1];

            foreach (DiscordEmbedField field in e.Fields) {
                builder.AddField(field.Name, field.Value, field.Inline);
            }
            if (e.Title != null) builder.Title = e.Title;
            if (e.Description != null) builder.Description = e.Description;
            if (e.Color != null) builder.Color = e.Color;
            else builder.Color = Color;
            if (e.Thumbnail != null) builder.ThumbnailUrl = e.Thumbnail.Url.ToString();
            if (e.Thumbnail != null) builder.Timestamp = e.Timestamp;
            if (e.Author != null) builder.WithAuthor(e.Author.Name != null ? e.Author.Name : null, 
                e.Author.Url != null ? e.Author.Url.ToString() : null, 
                e.Author.IconUrl != null ? e.Author.IconUrl.ToString() : null);

            if (ShowPageNumbers) builder.WithFooter($"Page {pageNum}/{Pages}" + (e.Footer != null ? $" | {e.Footer.Text}" : ""));
            else builder.WithFooter(e.Footer.Text);
            return builder.Build();
        }
    }
}
