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

namespace KekBot
{
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

            commands.CommandErrored += PrintError;

            commands.RegisterConverter(new ChoicesConverter());
            commands.RegisterUserFriendlyTypeName<PickCommand.ChoicesList>("string[]");

            commands.RegisterCommands<TestCommand>();
            commands.RegisterCommands<PickCommand>();

            await discord.ConnectAsync();
            await Task.Delay(-1);
        }

        private async static Task PrintError(CommandErrorEventArgs e)
        {
            await e.Context.Channel.SendMessageAsync($"An error occured: {e.Exception.Message}");
            Console.Error.WriteLine(e.Exception);
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

    public struct Config {
        [JsonProperty("token")]
        public string Token { get; private set; }
    }
}
