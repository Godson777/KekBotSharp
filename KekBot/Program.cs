using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using ImageMagick;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace KekBot {
    class Program {

        static DiscordClient discord;
        static CommandsNextExtension commands;
        static ConcurrentDictionary<ulong, string> PrefixSettings = new ConcurrentDictionary<ulong, string>();


        static void Main(string[] args) {
            PrefixSettings.AddOrUpdate(283100276125073409ul, "c#", (k, vold) => "p$");
            PrefixSettings.AddOrUpdate(421681525227257878ul, "d$", (k, vold) => "p$");
            MainAsync(args).ConfigureAwait(false).GetAwaiter().GetResult();
        }

        static async Task MainAsync(string[] args) {
            discord = new DiscordClient(new DiscordConfiguration {
                Token = "MjgxOTM2OTQ2MjE5OTc0Njcw.XllvcQ.f6cCxFKmDn1-vMXMNE79u7wa4E8",
                TokenType = TokenType.Bot,
                LogLevel = LogLevel.Debug,
                UseInternalLogHandler = true
            });

            commands = discord.UseCommandsNext(new CommandsNextConfiguration {
                EnableMentionPrefix = true,
                PrefixResolver = ResolvePrefixAsync
            });

            commands.RegisterCommands<TestCommand>();

            await discord.ConnectAsync();
            await Task.Delay(-1);
        }

        private static Task<int> ResolvePrefixAsync(DiscordMessage msg) {
            var guildId = msg.Channel.GuildId;
            if (guildId == 0) return Task.FromResult(-1);

            var pLen = PrefixSettings.TryGetValue(guildId, out string prefix)
                ? msg.GetStringPrefixLength(prefix)
                : msg.GetStringPrefixLength("p$");

            return Task.FromResult(pLen);
        }
    }
}
