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

        static DiscordClient? Discord;
        static CommandsNextExtension? Commands;
        static readonly ConcurrentDictionary<ulong, string> PrefixSettings = new ConcurrentDictionary<ulong, string>();
        static InteractivityExtension? Interactivity;

        static void Main(string[] args) {
            PrefixSettings.AddOrUpdate(283100276125073409ul, "c#", (k, vold) => "p$");
            PrefixSettings.AddOrUpdate(421681525227257878ul, "d$", (k, vold) => "p$");
            MainAsync(args).ConfigureAwait(false).GetAwaiter().GetResult();
        }

        static async Task MainAsync(string[] _) {
            Config config = await Config.Get();
            Discord = new DiscordClient(new DiscordConfiguration {
                Token = config.Token,
                TokenType = TokenType.Bot,
                LogLevel = LogLevel.Debug,
                UseInternalLogHandler = true
            });

            Interactivity = Discord.UseInteractivity(new InteractivityConfiguration());

            Commands = Discord.UseCommandsNext(new CommandsNextConfiguration {
                EnableMentionPrefix = true,
                PrefixResolver = ResolvePrefixAsync,
                IgnoreExtraArguments = true,
                EnableDefaultHelp = false
            });

            Commands.CommandErrored += PrintError;

            Commands.RegisterConverter(new ChoicesConverter());
            Commands.RegisterUserFriendlyTypeName<PickCommand.ChoicesList>("string[]");

            Commands.RegisterCommands<TestCommand>();
            Commands.RegisterCommands<PickCommand>();
            Commands.RegisterCommands<PingCommand>();
            Commands.RegisterCommands<OwnerCommands>();
            Commands.RegisterCommands<TestCommandTwo>();
            Commands.RegisterCommands<HelpCommand>();

            await Discord.ConnectAsync();
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

        
    }

    internal struct Config {
        [JsonProperty("token")]
        public string Token { get; private set; }
        [JsonProperty("database")]
        public string Db { get; private set; }
        [JsonProperty("dbUser")]
        public string DbUser { get; private set; }
        [JsonProperty("dbPassword")]
        public string DbPass { get; private set; }
        [JsonProperty("botOwner")]
        public ulong BotOwner { get; private set; }

        public static async Task<Config> Get() {
            //load config
            var json = "";
            /*
             * @todo Adjust Config.Get()
             * @body This needs to be edited later, either the location of config.json should be a commandline argument, or we simply have a commandline argument named --dev or something to differentiate the environment KekBot is in. That way we can test but still have all the proper resources and stuff where they need to be.
             */
            using (FileStream fs = File.OpenRead("../../../../config/config.json"))
            using (StreamReader sr = new StreamReader(fs, new UTF8Encoding(false)))
                json = await sr.ReadToEndAsync();

            return JsonConvert.DeserializeObject<Config>(json);
        }
    }
}
