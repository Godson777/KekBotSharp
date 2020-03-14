using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KekBot.Menu {
    class Paginator : Menu {
        public Func<int, int, DiscordColor> Color { private get; set; }
        public Func<int, int, string> Text { private get; set; }
        public int Columns { private get; set; } = 1;
        public int ItemsPerPage { private get; set; } = 10;
        public bool ShowPageNumbers { private get; set; } = true;
        public bool NumberedItems { private get; set; } = true;
        public List<string> Strings { get; set; } = new List<string>();
        private int Pages => (int)Math.Ceiling((double)Strings.Count / ItemsPerPage);
        public Action<DiscordMessage>? FinalAction { private get; set; } = async m => await m.DeleteAllReactionsAsync();
        public int BulkSkipNumber { private get; set; } = 0;
        public bool WrapPageEnds { private get; set; } = true;

        protected string BigLeft { get; private set; } = "⏪";
        protected string Left { get; private set; } = "◀️";
        protected string Stop { get; private set; } = "⏹";
        protected string Right { get; private set; } = "▶";
        protected string BigRight { get; private set; } = "⏩";
        protected string[] LeftRightStop() => new string[] { Left, Right, Stop };
        protected string[] BigLeftRight() => new string[] { BigLeft, BigRight };

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
            else if (pageNum > Pages)
                pageNum = Pages;
            var msg = RenderPage(pageNum);
            await Initialize(await channel.SendMessageAsync(content: Text?.Invoke(pageNum, Pages), embed: msg), pageNum);
        }

        private async Task Paginate(DiscordMessage message, int pageNum) {
            if (pageNum < 1)
                pageNum = 1;
            else if (pageNum > Pages)
                pageNum = Pages;
            var msg = RenderPage(pageNum);
            await Initialize(await message.ModifyAsync(content: Text?.Invoke(pageNum, Pages), embed: msg), pageNum);
        }

        private async Task Initialize(DiscordMessage message, int pageNum) {
            if (Pages > 1) {
                if (BulkSkipNumber > 1) {
                    await message.CreateReactionAsync(DiscordEmoji.FromUnicode(BigLeft));
                }

                await message.CreateReactionAsync(DiscordEmoji.FromUnicode(Left));
                await message.CreateReactionAsync(DiscordEmoji.FromUnicode(Stop));

                if (BulkSkipNumber > 1) {
                    await message.CreateReactionAsync(DiscordEmoji.FromUnicode(Right));
                }

                await message.CreateReactionAsync(DiscordEmoji.FromUnicode(BulkSkipNumber > 1 ? BigRight : Right));
            } else {
                await message.CreateReactionAsync(DiscordEmoji.FromUnicode(Stop));
            }
            await Pagination(message, pageNum);
        }

        private async Task Pagination(DiscordMessage message, int pageNum) {
            var result = await Interactivity.WaitForReactionAsync(react => {
                if (react.Message.Id != message.Id) return false;
                if (LeftRightStop().Contains(react.Emoji.Name)) return IsValidUser(react.User, react.Guild);
                if (BigLeftRight().Contains(react.Emoji.Name)) return BulkSkipNumber > 1 && IsValidUser(react.User, react.Guild);
                return false;
            }, Timeout);

            if (result.TimedOut) {
                FinalAction?.Invoke(message);
                return;
            }

            var newPageNum = pageNum;
            var e = result.Result.Emoji.Name;
            if (e == Left) {
                if (newPageNum == 1 && WrapPageEnds) newPageNum = Pages + 1;
                if (newPageNum > 1) newPageNum--;
            } else if (e == Right) {
                if (newPageNum == Pages && WrapPageEnds) newPageNum = 0;
                if (newPageNum < Pages) newPageNum++;
            } else if (e == BigLeft) {
                if (newPageNum > 1 || WrapPageEnds) {
                    for (int i = 1; (newPageNum > 1 || WrapPageEnds) && i < BulkSkipNumber; i++) {
                        if (newPageNum == 1 && WrapPageEnds) newPageNum = Pages + 1;
                        newPageNum--;
                    }
                }
            } else if (e == BigRight) {
                if (newPageNum < Pages || WrapPageEnds) {
                    for (int i = 1; (newPageNum < Pages || WrapPageEnds) && i < BulkSkipNumber; i++) {
                        if (newPageNum == Pages && WrapPageEnds) newPageNum = 0;
                        newPageNum++;
                    }
                }
            } else if (e == Stop) {
                FinalAction?.Invoke(message);
                return;
            }

            await result.Result.Message.DeleteReactionAsync(result.Result.Emoji, result.Result.User);
            var m = await message.ModifyAsync(content: Text?.Invoke(pageNum, Pages), embed: RenderPage(newPageNum));
            await Pagination(m, newPageNum);
        }

        private DiscordEmbed RenderPage(int pageNum) {
            var builder = new DiscordEmbedBuilder();
            int start = (pageNum - 1) * ItemsPerPage;
            int end = Strings.Count < pageNum * ItemsPerPage ? Strings.Count : pageNum * ItemsPerPage;
            if (Columns == 1) {
                var sbuilder = new StringBuilder();
                for (int i = start; i < end; i++)
                    sbuilder.Append("\n").Append(NumberedItems ? $"`{i + 1}.` " : "").Append(Strings[i]);
                builder.Description = sbuilder.ToString();
            } else {
                int per = (int)Math.Ceiling((double)(end - start) / Columns);
                for (int k = 0; k < Columns; k++) {
                    var sbuilder = new StringBuilder();
                    for (int i = start + k * per; i < end && i < start + (k + 1) * per; i++)
                        sbuilder.Append("\n").Append(NumberedItems ? $"{i + 1}. " : "").Append(Strings[i]);
                    builder.AddField("", sbuilder.ToString(), true);
                }
            }

            if (Color != null) builder.Color = Color.Invoke(pageNum, Pages);
            if (ShowPageNumbers)
                builder.WithFooter($"Page {pageNum}/{Pages}");
            return builder.Build();
        }

        public void SetGenericColor(DiscordColor color) {
            this.Color = (i0, i1) => {
                return color;
            };
        }

        public void SetGenericText(string text) {
            this.Text = (i0, i1) => {
                return text;
            };
        }
    }
}
