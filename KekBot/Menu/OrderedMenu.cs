using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace KekBot.Menu {
    class OrderedMenu : Menu {
        public DiscordColor Color { private get; set; }
        public string Text { private get; set; }
        public string Description { private get; set; }
        public List<string> Choices { get; private set; } = new List<string>();
        public Action<DiscordMessage, int> Action { private get; set; }
        public Action<DiscordMessage> CancelAction { private get; set; }

        private readonly string[] Numbers = { "1⃣", "2⃣", "3⃣", "4⃣", "5⃣", "6⃣", "7⃣", "8⃣", "9⃣", "🔟" };

        private readonly string Cancel = "❌";

        public OrderedMenu(InteractivityExtension interactivity) : base(interactivity) {
        }
        //TODO: finish literally this entire class LMAO
        public override Task Display(DiscordChannel channel) {
            throw new NotImplementedException();
        }

        public override Task Display(DiscordMessage message) {
            throw new NotImplementedException();
        }
    }
}
