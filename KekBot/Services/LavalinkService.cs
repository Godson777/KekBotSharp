using DSharpPlus;
using DSharpPlus.EventArgs;
using DSharpPlus.Lavalink;
using DSharpPlus.Net;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace KekBot.Services {
    public sealed class LavalinkService {
        public LavalinkNodeConnection LavalinkNode { get; private set; }

        public LavalinkService(DiscordClient Client) {
            Client.Ready += Ready;
        }

        private async Task Ready(DiscordClient client, ReadyEventArgs e) {
            if (this.LavalinkNode != null) return;

            var lava = client.GetLavalink();
            this.LavalinkNode = await lava.ConnectAsync(new LavalinkConfiguration {
                Password = "youshallnotpass",

                SocketEndpoint = new ConnectionEndpoint("localhost", 2333),
                RestEndpoint = new ConnectionEndpoint("localhost", 2333)
            });
        }
    }
}
