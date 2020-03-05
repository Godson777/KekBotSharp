using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.ConstrainedExecution;
using System.Text;
using System.Threading.Tasks;

namespace KekBot.Menu {
    public abstract class Menu {
        public HashSet<ulong> users { get; private set; } = new HashSet<ulong>();
        public HashSet<ulong> roles { get; private set; } = new HashSet<ulong>();
        public TimeSpan timeout { get; set; } = TimeSpan.FromMinutes(5);
        protected InteractivityExtension interactivity { get; set; }

        public Menu(InteractivityExtension interactivity) {
            this.interactivity = interactivity;
        }

        public abstract Task Display(DiscordChannel channel);

        public abstract Task Display(DiscordMessage message);

        protected bool isValidUser(DiscordUser user) {
            return this.isValidUser(user, null);
        }

        protected bool isValidUser(DiscordUser user, [AllowNull] DiscordGuild guild) {
            if (user.IsBot) {
                return false;
            } else if (this.users.Count == 0 && this.roles.Count == 0) {
                return true;
            } else if (this.users.Contains(user.Id)) {
                return true;
            } else if (guild != null && guild.Members.ContainsKey(user.Id)) {
                var roles = (user as DiscordMember).Roles;
                return roles.Select(x => x.Id).Intersect(this.roles).Any();
            } else {
                return false;
            }
        }
    }
}
