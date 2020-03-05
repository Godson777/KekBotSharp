using System;
using System.Collections.Concurrent;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using Newtonsoft.Json;
using KekBot.ArgumentResolvers;
using DSharpPlus.Interactivity;
using KekBot.Command.Commands;

namespace KekBot {
    class Program {

        static DiscordClient? discord;
        static CommandsNextExtension? commands;
        static ConcurrentDictionary<ulong, string> PrefixSettings = new ConcurrentDictionary<ulong, string>();
        static InteractivityExtension? interactivity;

        static void Main(string[] args) {
            PrefixSettings.AddOrUpdate(283100276125073409ul, "c#", (k, vold) => "p$");
            PrefixSettings.AddOrUpdate(421681525227257878ul, "d$", (k, vold) => "p$");
            MainAsync(args).ConfigureAwait(false).GetAwaiter().GetResult();
        }

        static async Task MainAsync(string[] args) {
            Config config = await getConfig();
            discord = new DiscordClient(new DiscordConfiguration {
                Token = config.Token,
                TokenType = TokenType.Bot,
                LogLevel = LogLevel.Debug,
                UseInternalLogHandler = true
            });

            interactivity = discord.UseInteractivity(new InteractivityConfiguration());

            commands = discord.UseCommandsNext(new CommandsNextConfiguration {
                EnableMentionPrefix = true,
                PrefixResolver = ResolvePrefixAsync,
                IgnoreExtraArguments = true,
                EnableDefaultHelp = false
            });

            commands.CommandErrored += PrintError;

            commands.RegisterConverter(new ChoicesConverter());
            commands.RegisterUserFriendlyTypeName<PickCommand.ChoicesList>("string[]");

            commands.RegisterCommands<TestCommand>();
            commands.RegisterCommands<PickCommand>();
            commands.RegisterCommands<PingCommand>();
            commands.RegisterCommands<OwnerCommands>();
            commands.RegisterCommands<TestCommandTwo>();
            commands.RegisterCommands<HelpCommand>();

            await discord.ConnectAsync();
            await Task.Delay(-1);
        }

        private async static Task PrintError(CommandErrorEventArgs e) {
            await e.Context.Channel.SendMessageAsync($"An error occured: {e.Exception.Message}");
            Console.Error.WriteLine(e.Exception);
        }

        private static Task<int> ResolvePrefixAsync(DiscordMessage msg) {
            var guildId = msg.Channel.GuildId;
            if (guildId == 0) return Task.FromResult(-1);

            var pLen = PrefixSettings.TryGetValue(guildId, out string? prefix) && prefix != null
                ? msg.GetStringPrefixLength(prefix)
                : msg.GetStringPrefixLength("p$");

            return Task.FromResult(pLen);
        }

        public static async Task<Config> getConfig() {
            //load config
            var json = "";
            using (FileStream fs = File.OpenRead("../../../../config/config.json"))
            using (StreamReader sr = new StreamReader(fs, new UTF8Encoding(false)))
                json = await sr.ReadToEndAsync();

            return JsonConvert.DeserializeObject<Config>(json);
        }
    }

    public struct Config {
        [JsonProperty("token")]
        public string Token { get; private set; }
        [JsonProperty("database")]
        public string db { get; private set; }
        [JsonProperty("dbUser")]
        public string dbUser { get; private set; }
        [JsonProperty("dbPassword")]
        public string dbPass { get; private set; }
        [JsonProperty("botOwner")]
        public ulong botOwner { get; private set; }
    }
}
