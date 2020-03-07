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
        public HashSet<ulong> Users { get; private set; } = new HashSet<ulong>();
        public HashSet<ulong> Roles { get; private set; } = new HashSet<ulong>();
        public TimeSpan Timeout { get; set; } = TimeSpan.FromMinutes(5);
        protected InteractivityExtension Interactivity { get; set; }

        public Menu(InteractivityExtension interactivity) {
            this.Interactivity = interactivity;
        }

        public abstract Task Display(DiscordChannel channel);

        public abstract Task Display(DiscordMessage message);

        protected bool IsValidUser(DiscordUser user) {
            return this.IsValidUser(user, null);
        }

        protected bool IsValidUser(DiscordUser user, [AllowNull] DiscordGuild guild) {
            if (user.IsBot) {
                return false;
            } else if (this.Users.Count == 0 && this.Roles.Count == 0) {
                return true;
            } else if (this.Users.Contains(user.Id)) {
                return true;
            } else if (guild != null && guild.Members.ContainsKey(user.Id)) {
                var roles = (user as DiscordMember).Roles;
                return roles.Select(x => x.Id).Intersect(this.Roles).Any();
            } else {
                return false;
            }
        }
    }
}
