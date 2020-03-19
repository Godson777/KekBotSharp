using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using ImageMagick;
using RethinkDb.Driver.Ast;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KekBot.Menu {
    class OrderedMenu : Menu {
        public DiscordColor Color { private get; set; }
        public string Text { private get; set; }
        public string Description { private get; set; }
        public List<string> Choices { get; private set; } = new List<string>();
        public Action<DiscordMessage, int> Action { private get; set; }
        public Action<DiscordMessage> CancelAction { private get; set; } = async x => await x.DeleteAllReactionsAsync();

        private readonly string[] Numbers = { "1⃣", "2⃣", "3⃣", "4⃣", "5⃣", "6⃣", "7⃣", "8⃣", "9⃣", "🔟" };

        private readonly string Cancel = "❌";

        public OrderedMenu(InteractivityExtension interactivity) : base(interactivity) {
        }
        
        public override async Task Display(DiscordChannel channel) {
            DisplayChecks();
            await Initialize(await channel.SendMessageAsync(content: Text, embed: GetMessage()));
        }

        public override async Task Display(DiscordMessage message) {
            DisplayChecks();
            await Initialize(await message.ModifyAsync(content: Text, embed: GetMessage()));
        }

        private protected override void DisplayChecks() {
            if (Users.Count == 0 && Roles.Count == 0) throw new MenuFailedException();
            if (Choices.Count == 0) throw new MenuFailedException("Must have more than one choice.");
            if (Action == null) throw new MenuFailedException("Must have no more than 10 choices.");
            if (Text == null && Description == null) throw new MenuFailedException("Either text or description must be provided.");
        }

        private async Task Initialize(DiscordMessage message) {
            for (int i = 0; i < Choices.Count; i++) {
                if (i < Choices.Count - 1) await message.CreateReactionAsync(DiscordEmoji.FromUnicode(Numbers[i]));
                else {
                    await message.CreateReactionAsync(DiscordEmoji.FromUnicode(Numbers[i]));
                    await message.CreateReactionAsync(DiscordEmoji.FromUnicode(Cancel));
                }
            }
            var result = await Interactivity.WaitForReactionAsync(react => {
                if (react.Message.Id != message.Id) return false;
                if (Numbers.Contains(react.Emoji.Name) || react.Emoji.Name == Cancel) return IsValidUser(react.User, react.Guild);
                return false;
            }, Timeout);

            if (result.TimedOut) {
                CancelAction?.Invoke(message);
                return;
            }

            if (result.Result.Emoji.Name == Cancel) CancelAction.Invoke(message);
            else Action?.Invoke(message, GetNumber(result.Result.Emoji.Name));
        }

        private int GetNumber(string emoji) {
            for (int i = 0; i < Numbers.Count(); i++)
                if (Numbers[i].Equals(emoji))
                    return i + 1;
            return -1;
        }

        private DiscordEmbed GetMessage() {
            var builder = new DiscordEmbedBuilder();

            var sb = new StringBuilder();
            for (int i = 0; i < Choices.Count; i++) sb.Append($"\n{Numbers[i]} {Choices[i]}");
            builder.Color = Color;
            builder.Description = Description == null ? sb.ToString() : Description + sb.ToString();

            return builder.Build();
        }
    }
}
