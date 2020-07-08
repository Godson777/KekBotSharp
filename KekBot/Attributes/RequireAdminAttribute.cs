using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace KekBot.Attributes {
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    public sealed class RequireAdminAttribute : CheckBaseAttribute {

        public RequireAdminAttribute() { }

        public override async Task<bool> ExecuteCheckAsync(CommandContext ctx, bool help) {
            Config config = await Config.Get();
            if (config.RankedUsers.ContainsKey(ctx.User.Id)) {
                if (config.RankedUsers[ctx.User.Id] >= Rank.Admin) return true;
                else return false;
            } else return false;
        }
    }
}
