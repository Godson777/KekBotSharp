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
using Newtonsoft.Json.Serialization;
using System.Text.Json.Serialization;
using System.Collections.Generic;
using DSharpPlus.CommandsNext.Exceptions;

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
            Commands.RegisterCommands<FunCommands>();

            await Discord.ConnectAsync();
            await Task.Delay(-1);
        }

        private async static Task PrintError(CommandErrorEventArgs e) {
            if (e.Exception is CommandNotFoundException) return;
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
        /*
         * @todo Adjust collecting resources like config.json and others.
         * @body This needs to be edited later, either the location of all the resources should be a commandline argument, or we simply have a commandline argument named --dev or something to differentiate the environment KekBot is in. That way we can test but still have all the proper resources and stuff where they need to be.
         */
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
            using var fs = File.OpenRead("../../../../config/config.json");
            using var sr = new StreamReader(fs, new UTF8Encoding(false));
            return JsonConvert.DeserializeObject<Config>(await sr.ReadToEndAsync());
        }
    }

    internal struct CustomEmote {
        //All the json values
        [JsonProperty("thinkings")]
        public List<string> Thinkings;
        [JsonProperty("dances")]
        public List<string> Dances;
        [JsonProperty("topkek")]
        public string Topkek { get; }
        [JsonProperty("gold_trophy")]
        public string GoldTrophy { get; }
        [JsonProperty("silver_trophy")]
        public string SilverTrophy { get; }
        [JsonProperty("bronze_trophy")]
        public string BronzeTrophy { get; }
        [JsonProperty("loadings")]
        public List<string> Loadings;

        //Trying something clever I hope
        public static Random random = new Random();
        public string Think { get {
                return Thinkings[random.Next(Thinkings.Count)];
            } }
        public string Dance { get {
                return Dances[random.Next(Dances.Count)];
            } }
        public string Loading { get {
                return Loadings[random.Next(Loadings.Count)];
            } }

        public static async Task<CustomEmote> Get() {
            using var fs = File.OpenRead("../../../../config/emotes.json");
            using var sr = new StreamReader(fs, new UTF8Encoding(false));
            return JsonConvert.DeserializeObject<CustomEmote>(await sr.ReadToEndAsync());
        }


    }
}
