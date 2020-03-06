using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace KekBot.Menu {
    class Paginator : Menu {
        public Func<int, int, DiscordColor> color { private get; set; }
        public Func<int, int, String> text { private get; set; }
        public int columns { private get; set; } = 1;
        public int itemsPerPage { private get; set; } = 10;
        public bool showPageNumbers { private get; set; } = true;
        public bool numberItems { private get; set; } = true;
        public List<string> strings { get; private set; } = new List<string>();
        private int pages { get {
                return (int)Math.Ceiling((double)this.strings.Count / itemsPerPage);
            } }
        public Action<DiscordMessage>? finalAction { private get; set; }
        public int bulkSkipNumber { private get; set; } = 0;
        public bool wrapPageEnds { private get; set; } = true;

        protected string BIG_LEFT { get; set; } = "⏪";
        protected string LEFT { get; set; } = "◀️";
        protected string STOP { get; set; } = "⏹";
        protected string RIGHT { get; set; } = "▶";
        protected string BIG_RIGHT { get; set; } = "⏩";

        public Paginator(InteractivityExtension interactivity) : base(interactivity) {
        }

        public override async Task Display(DiscordChannel channel) {
            //Todo: Implement permission checks
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
                if (bulkSkipNumber > 1) {
                    await message.CreateReactionAsync(DiscordEmoji.FromUnicode(BIG_LEFT));
                }

                await message.CreateReactionAsync(DiscordEmoji.FromUnicode(LEFT));
                await message.CreateReactionAsync(DiscordEmoji.FromUnicode(STOP));

                if (bulkSkipNumber > 1) {
                    await message.CreateReactionAsync(DiscordEmoji.FromUnicode(RIGHT));
                }

                await message.CreateReactionAsync(DiscordEmoji.FromUnicode(bulkSkipNumber > 1 ? BIG_RIGHT : RIGHT));
            } else {
                await message.CreateReactionAsync(DiscordEmoji.FromUnicode(STOP));
            }
            await Pagination(message, pageNum);
        }

        private async Task Pagination(DiscordMessage message, int pageNum) {
            var result = await interactivity.WaitForReactionAsync(react => {
                if (react.Message.Id != message.Id) return false;
                if (LEFT.Equals(react.Emoji.Name) || STOP.Equals(react.Emoji.Name) || RIGHT.Equals(react.Emoji.Name)) return isValidUser(react.User, react.Guild);
                if (BIG_LEFT.Equals(react.Emoji.Name) || BIG_RIGHT.Equals(react.Emoji.Name)) return bulkSkipNumber > 1 && isValidUser(react.User, react.Guild);
                return false;
            }, timeout);

            if (result.TimedOut) {
                finalAction.Invoke(message);
                return;
            }

            var newPageNum = pageNum;
            var e = result.Result.Emoji.Name;
            if (e.Equals(LEFT)) {
                if (newPageNum == 1 && wrapPageEnds) newPageNum = pages + 1;
                if (newPageNum > 1) newPageNum--;
            }
            if (e.Equals(RIGHT)) {
                if (newPageNum == pages && wrapPageEnds) newPageNum = 0;
                if (newPageNum < pages) newPageNum++;
            }
            if (e.Equals(BIG_LEFT)) {
                if (newPageNum > 1 || wrapPageEnds) {
                    for (int i = 1; (newPageNum > 1 || wrapPageEnds) && i < bulkSkipNumber; i++) {
                        if (newPageNum == 1 && wrapPageEnds) newPageNum = pages + 1;
                        newPageNum--;
                    }
                }
            }
            if (e.Equals(BIG_RIGHT)) {
                if (newPageNum < pages || wrapPageEnds) {
                    for (int i = 1; (newPageNum < pages || wrapPageEnds) && i < bulkSkipNumber; i++) {
                        if (newPageNum == pages && wrapPageEnds) newPageNum = 0;
                        newPageNum++;
                    }
                }
            }
            if (e.Equals(STOP)) {
                finalAction.Invoke(message);
                return;
            }

            await result.Result.Message.DeleteReactionAsync(result.Result.Emoji, result.Result.User);
            var m = await message.ModifyAsync(content: text != null ? text.Invoke(pageNum, pages) : null, embed: RenderPage(newPageNum));
            await Pagination(m, newPageNum);
        }

        private DiscordEmbed RenderPage(int pageNum) {
            DiscordEmbedBuilder builder = new DiscordEmbedBuilder();
            int start = (pageNum - 1) * itemsPerPage;
            int end = strings.Count < pageNum * itemsPerPage ? strings.Count : pageNum * itemsPerPage;
            if (columns == 1) {
                StringBuilder sbuilder = new StringBuilder();
                for (int i = start; i < end; i++)
                    sbuilder.Append("\n").Append(numberItems ? $"`{i+1}.` " : "").Append(strings[i]);
                builder.Description = sbuilder.ToString();
            } else {
                int per = (int)Math.Ceiling((double)(end - start) / columns);
                for (int k = 0; k < columns; k++) {
                    StringBuilder sbuilder = new StringBuilder();
                    for (int i = start + k * per; i < end && i < start + (k + 1) * per; i++)
                        sbuilder.Append("\n").Append(numberItems ? $"{i + 1}. " : "").Append(strings[i]);
                    builder.AddField("", sbuilder.ToString(), true);
                }
            }

            if (color != null) builder.Color = color.Invoke(pageNum, pages);
            if (showPageNumbers)
                builder.WithFooter($"Page {pageNum}/{pages}");
            return builder.Build();
        }
    }
}
