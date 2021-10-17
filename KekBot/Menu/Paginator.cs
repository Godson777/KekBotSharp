using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.Interactivity;
using RethinkDb.Driver.Model;
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
        public int BulkSkipNumber { private get; set; } = 0;
        public bool WrapPageEnds { private get; set; } = true;

        protected DiscordButtonComponent BigLeftBtn { get; private set; } = new DiscordButtonComponent(DSharpPlus.ButtonStyle.Primary, "paginator_big_left", null, emoji: new DiscordComponentEmoji("⏪"));
        protected DiscordButtonComponent LeftBtn { get; private set; } = new DiscordButtonComponent(DSharpPlus.ButtonStyle.Primary, "paginator_left", null, emoji: new DiscordComponentEmoji("◀️"));
        protected DiscordButtonComponent StopBtn { get; private set; } = new DiscordButtonComponent(DSharpPlus.ButtonStyle.Primary, "paginator_stop", null, emoji: new DiscordComponentEmoji("⏹"));
        protected DiscordButtonComponent RightBtn { get; private set; } = new DiscordButtonComponent(DSharpPlus.ButtonStyle.Primary, "paginator_right", null, emoji: new DiscordComponentEmoji("▶"));
        protected DiscordButtonComponent BigRightBtn { get; private set; } = new DiscordButtonComponent(DSharpPlus.ButtonStyle.Primary, "paginator_big_right", null, emoji: new DiscordComponentEmoji("⏩"));

        public Paginator(InteractivityExtension interactivity) : base(interactivity) {
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
            if (Strings.Count == 0) throw new MenuFailedException();
        }

        private async Task Paginate(DiscordChannel channel, int pageNum) {
            if (pageNum < 1)
                pageNum = 1;
            else if (pageNum > Pages)
                pageNum = Pages;
            var msg = RenderPage(pageNum);
            await Pagination(await channel.SendMessageAsync(
                new DiscordMessageBuilder()
                .WithContent(Text?.Invoke(pageNum, Pages))
                .WithEmbed(msg)
                .AddComponents(GetButtonList())), pageNum);
            //await Initialize(await channel.SendMessageAsync(content: Text?.Invoke(pageNum, Pages), embed: msg), pageNum);
        }

        private async Task Paginate(DiscordMessage message, int pageNum) {
            if (pageNum < 1)
                pageNum = 1;
            else if (pageNum > Pages)
                pageNum = Pages;
            var msg = RenderPage(pageNum);
            await Pagination(await message.ModifyAsync(
                new DiscordMessageBuilder()
                .WithContent(Text?.Invoke(pageNum, Pages))
                .WithEmbed(msg)
                .AddComponents(GetButtonList())), pageNum);
        }

        private DiscordComponent[] GetButtonList() {
            var buttons = new List<DiscordComponent>();
            if (Pages > 1) {
                if (BulkSkipNumber > 1) {
                    buttons.Add(BigLeftBtn);
                }

                buttons.Add(LeftBtn);
                buttons.Add(StopBtn);
                buttons.Add(RightBtn);

                if (BulkSkipNumber > 1) {
                    buttons.Add(BigRightBtn);
                }
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
            } else if (e == BigLeftBtn.CustomId) {
                if (newPageNum > 1 || WrapPageEnds) {
                    for (int i = 1; (newPageNum > 1 || WrapPageEnds) && i < BulkSkipNumber; i++) {
                        if (newPageNum == 1 && WrapPageEnds) newPageNum = Pages + 1;
                        newPageNum--;
                    }
                }
            } else if (e == BigRightBtn.CustomId) {
                if (newPageNum < Pages || WrapPageEnds) {
                    for (int i = 1; (newPageNum < Pages || WrapPageEnds) && i < BulkSkipNumber; i++) {
                        if (newPageNum == Pages && WrapPageEnds) newPageNum = 0;
                        newPageNum++;
                    }
                }
            } else if (e == StopBtn.CustomId) {
                await result.Result.Interaction.CreateResponseAsync(InteractionResponseType.DeferredMessageUpdate);
                FinalAction?.Invoke(result.Result.Message);
                return;
            }
            //await result.Result.Message.DeleteReactionAsync(result.Result.Emoji, result.Result.User
            await result.Result.Interaction.CreateResponseAsync(InteractionResponseType.UpdateMessage, new DiscordInteractionResponseBuilder().WithContent(Text?.Invoke(pageNum, Pages))
                .AddEmbed(RenderPage(newPageNum))
                .AddComponents(GetButtonList()));
            await Pagination(result.Result.Message, newPageNum);
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
