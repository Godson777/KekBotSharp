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
using RethinkDb.Driver;
using RethinkDb.Driver.Net;
using System.Text.RegularExpressions;
using KekBot.Utils;

namespace KekBot {
    class Program {

        static DiscordClient? Discord;
        static CommandsNextExtension? Commands;
        static readonly ConcurrentDictionary<ulong, string> PrefixSettings = new ConcurrentDictionary<ulong, string>();
        static InteractivityExtension? Interactivity;
        public static RethinkDB R = RethinkDB.R;
        public static Connection conn;


        static void Main(string[] args) {
            PrefixSettings.AddOrUpdate(283100276125073409ul, "c#", (k, vold) => "p$");
            PrefixSettings.AddOrUpdate(421681525227257878ul, "d$", (k, vold) => "p$");
            MainAsync(args).ConfigureAwait(false).GetAwaiter().GetResult();
        }

        static async Task MainAsync(string[] _) {
            var config = await Config.Get();

            try {
                conn = R.Connection().User(config.DbUser, config.DbPass).Connect();
            } catch (ReqlDriverError e) {
                Console.WriteLine("There was an error logging into rethinkdb, are you sure that it's on, or that you typed the info correctly?");
                //end process
            }

            if (config.Db == null) {
                Console.WriteLine("There was no database to use provided. Make sure \"database\" is in your config.json.");
                //end process
            }
            if (!(bool)R.DbList().Contains(config.Db).Run(conn)) {
                R.DbCreate(config.Db).Run(conn);
                Console.Write("Database wasn't found, so it was created.");
            }
            conn.Use(config.Db);

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

        /**
     * Verifies if the all the tables exist in our database.
     */
        private static void verifyTables() {
            if (!(bool)R.TableList().Contains("Profiles").Run(conn)) {
                Console.WriteLine("\"Profiles\" table was not found, so it is being made.");
                R.TableCreate("Profiles").OptArg("primary_key", "User ID").Run(conn);
            }
            if (!(bool)R.TableList().Contains("Responses").Run(conn)) {
                Console.WriteLine("\"Responses\" table was not found, so it is being made.");
                R.TableCreate("Responses").OptArg("primary_key", "Action").Run(conn);
            }
            if (!(bool)R.TableList().Contains("Settings").Run(conn)) {
                Console.WriteLine("\"Settings\" table was not found, so it is being made.");
                R.TableCreate("Settings").OptArg("primary_key", "Guild ID").Run(conn);
            }
            if (!(bool)R.TableList().Contains("Takeovers").Run(conn)) {
                Console.WriteLine("\"Takeovers\" table was not found, so it is being made.");
                R.TableCreate("Takeovers").OptArg("primary_key", "Name").Run(conn);
            }
            if (!(bool)R.TableList().Contains("Tickets").Run(conn)) {
                Console.WriteLine("\"Tickets\" table was not found, so it is being made.");
                R.TableCreate("Tickets").OptArg("primary_key", "ID").Run(conn);
            }
            if (!(bool)R.TableList().Contains("Twitter").Run(conn)) {
                Console.WriteLine("\"Twitter\" table was not found, so it is being made.");
                R.TableCreate("Twitter").OptArg("primary_key", "Account ID").Run(conn);
            }
        }
        /*
         * @todo Adjust collecting resources like config.json and others.
         * @body This needs to be edited later, either the location of all the resources should be a commandline argument, or we simply have a commandline argument named --dev or something to differentiate the environment KekBot is in. That way we can test but still have all the proper resources and stuff where they need to be.
         */
    }

    //Direct port from Java version with minor edits, could probably be optimized better.
    internal class Version {
        private int MajorVersion;
        private int MinorVersion;
        private int PatchVersion;
        private int BetaVersion;

        public string VersionString { get {
                return $"{MajorVersion}.{MinorVersion}.{PatchVersion}" + (BetaVersion > 0 ? $"-BETA{BetaVersion}" : "");
            } }

        public Version(int majorVersion, int minorVersion, int patchVersion, int betaVersion) {
            this.MajorVersion = majorVersion;
            this.MinorVersion = minorVersion;
            this.PatchVersion = patchVersion;
            this.BetaVersion = betaVersion;
        }

        public Version(int majorVersion, int minorVersion, int patchVersion) {
            this.MajorVersion = majorVersion;
            this.MinorVersion = minorVersion;
            this.PatchVersion = patchVersion;
            this.BetaVersion = 0;
        }

        public Version(int majorVersion, int minorVersion) {
            this.MajorVersion = majorVersion;
            this.MinorVersion = minorVersion;
            this.PatchVersion = 0;
            this.BetaVersion = 0;
        }

        Version(int majorVersion) {
            this.MajorVersion = majorVersion;
            this.MinorVersion = 0;
            this.PatchVersion = 0;
            this.BetaVersion = 0;
        }

        public static Version fromString(String version) {
            String[] parts = Regex.Split(version, "\\.|-BETA");
            if (parts.Length == 4) {
                return new Version(Util.ParseInt(parts[0], 1), Util.ParseInt(parts[1], 0), Util.ParseInt(parts[2], 0), Util.ParseInt(parts[3], 1));
            } else if (parts.Length == 3) {
                return new Version(Util.ParseInt(parts[0], 1), Util.ParseInt(parts[1], 0), Util.ParseInt(parts[2], 0));
            } else if (parts.Length == 2) {
                return new Version(Util.ParseInt(parts[0], 1), Util.ParseInt(parts[1], 0));
            } else if (parts.Length == 1) {
                return new Version(Util.ParseInt(parts[0], 1));
            }
            return new Version(1);
        }

        public bool isHigherThan(Version? version) {
            if (version == null || this.MajorVersion > version.MajorVersion) {
                return true;
            } else if (this.MajorVersion == version.MajorVersion) {
                if (this.MinorVersion > version.MinorVersion) {
                    return true;
                } else if (this.MinorVersion == version.MinorVersion) {
                    if (this.PatchVersion > version.PatchVersion) {
                        return true;
                    } else if (this.PatchVersion == version.PatchVersion) {
                        if (this.PatchVersion > version.PatchVersion)
                            return true;
                    }
                }
            }
            return false;
        }
    }


    internal class Config {
        [JsonProperty("token")]
        public string Token { get; private set; }
        [JsonProperty("database")]
        public string Db { get; private set; }
        [JsonProperty("dbUser")]
        public string DbUser { get; private set; }
        [JsonProperty("dbPassword")]
        public string DbPass { get; private set; }

        private static Config _instance;

        public static async Task<Config> Get() {
            if (_instance == null) {
                using var fs = File.OpenRead("../../../../config/config.json");
                using var sr = new StreamReader(fs, new UTF8Encoding(false));
                return _instance = JsonConvert.DeserializeObject<Config>(await sr.ReadToEndAsync());
            } else return _instance;
        }
    }

    internal class CustomEmote {
        //All the json values
        [JsonProperty("thinkings")]
        private List<string> Thinkings;
        [JsonProperty("dances")]
        private List<string> Dances;
        [JsonProperty("topkek")]
        public string Topkek { get; }
        [JsonProperty("gold_trophy")]
        public string GoldTrophy { get; }
        [JsonProperty("silver_trophy")]
        public string SilverTrophy { get; }
        [JsonProperty("bronze_trophy")]
        public string BronzeTrophy { get; }
        [JsonProperty("loadings")]
        private List<string> Loadings;

        //Trying something clever I hope
        private Random random = new Random();
        public string Think { get {
                return Thinkings[random.Next(Thinkings.Count)];
            } }
        public string Dance { get {
                return Dances[random.Next(Dances.Count)];
            } }
        public string Loading { get {
                return Loadings[random.Next(Loadings.Count)];
            } }

        private static CustomEmote _instance;

        public static async Task<CustomEmote> Get() {
            if (_instance == null) {
                using var fs = File.OpenRead("../../../../config/emotes.json");
                using var sr = new StreamReader(fs, new UTF8Encoding(false));
                return _instance = JsonConvert.DeserializeObject<CustomEmote>(await sr.ReadToEndAsync());
            } else return _instance;
        }
    }

    public struct Profile {

    }

    public struct Settings {

    }
}
