using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KekBot.Menu {
    public abstract class Menu {
        public HashSet<ulong> Users { get; private set; } = new HashSet<ulong>();
        public HashSet<ulong> Roles { get; private set; } = new HashSet<ulong>();
        public TimeSpan Timeout { get; set; } = TimeSpan.FromMinutes(5);
        protected InteractivityExtension Interactivity { get; set; }

        public Menu(InteractivityExtension interactivity) {
            this.Interactivity = interactivity;
        }

        public abstract Task Display(DiscordChannel channel);

        public abstract Task Display(DiscordMessage message);

        private protected bool IsValidUser(DiscordUser user, DiscordGuild? guild = null) {
            if (user.IsBot) {
                return false;
            } else if (Users.Count == 0 && Roles.Count == 0) {
                return true;
            } else if (Users.Contains(user.Id)) {
                return true;
            } else if (guild != null && guild.Members.ContainsKey(user.Id)) {
                var member = user as DiscordMember ?? guild.Members[user.Id];
                return member.Roles.Any(r => Roles.Contains(r.Id));
            } else {
                return false;
            }
        }

        private protected abstract void DisplayChecks();
    }
}
