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
using Newtonsoft.Json;
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
            //load config
            var json = "";
            using (FileStream fs = File.OpenRead("config/config.json"))
            using (StreamReader sr = new StreamReader(fs, new UTF8Encoding(false)))
                json = await sr.ReadToEndAsync();

            Config config = JsonConvert.DeserializeObject<Config>(json);
            discord = new DiscordClient(new DiscordConfiguration {
                Token = config.Token,
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
            DiscordGuild guild = msg.Channel.Guild;
            if (guild == null) return Task.FromResult(-1);

            if (PrefixSettings.TryGetValue(guild.Id, out string prefix)) {
                int p = msg.GetStringPrefixLength(prefix);
                if (p != -1) return Task.FromResult(p);
            } else {
                int p = msg.GetStringPrefixLength("p$");
                if (p != -1) return Task.FromResult(p);
            }

            return Task.FromResult(-1);
        }
    }

    public struct Config {
        [JsonProperty("token")]
        public string Token { get; private set; }
    }
}
