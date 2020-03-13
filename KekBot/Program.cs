using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using RethinkDb.Driver;
using RethinkDb.Driver.Net;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Exceptions;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using KekBot.ArgumentResolvers;
using KekBot.Commands;
using KekBot.Utils;

namespace KekBot {
    class Program {

        static DiscordClient? Discord;
        static CommandsNextExtension? Commands;
        static readonly ConcurrentDictionary<ulong, string> PrefixSettings = new ConcurrentDictionary<ulong, string>();
        static InteractivityExtension? Interactivity;
        public static RethinkDB R = RethinkDB.R;
        public static Connection Conn;


        static void Main(string[] args) {
            PrefixSettings.AddOrUpdate(283100276125073409ul, "c#", (k, vold) => "p$");
            PrefixSettings.AddOrUpdate(421681525227257878ul, "d$", (k, vold) => "p$");
            MainAsync(args).ConfigureAwait(false).GetAwaiter().GetResult();
        }

        [SuppressMessage("Globalization", "CA1303:Do not pass literals as localized parameters", Justification = "Running the bot requires knowledge of English")]
        static async Task MainAsync(string[] _) {
            var config = await Config.Get();

            try {
                Conn = R.Connection().User(config.DbUser, config.DbPass).Connect();
            } catch (ReqlDriverError) {
                Util.Panic("There was an error logging into rethinkdb, are you sure that it's on, or that you typed the info correctly?");
            }

            if (config.Db == null) {
                Util.Panic("There was no database to use provided. Make sure \"database\" is in your config.json.");
            }

            if (!(bool)R.DbList().Contains(config.Db).Run(Conn)) {
                R.DbCreate(config.Db).Run(Conn);
                Console.WriteLine("Database wasn't found, so it was created.");
            }
            Conn.Use(config.Db);

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
            if (!(bool)R.TableList().Contains("Profiles").Run(Conn)) {
                Console.WriteLine("\"Profiles\" table was not found, so it is being made.");
                R.TableCreate("Profiles").OptArg("primary_key", "User ID").Run(Conn);
            }
            if (!(bool)R.TableList().Contains("Responses").Run(Conn)) {
                Console.WriteLine("\"Responses\" table was not found, so it is being made.");
                R.TableCreate("Responses").OptArg("primary_key", "Action").Run(Conn);
            }
            if (!(bool)R.TableList().Contains("Settings").Run(Conn)) {
                Console.WriteLine("\"Settings\" table was not found, so it is being made.");
                R.TableCreate("Settings").OptArg("primary_key", "Guild ID").Run(Conn);
            }
            if (!(bool)R.TableList().Contains("Takeovers").Run(Conn)) {
                Console.WriteLine("\"Takeovers\" table was not found, so it is being made.");
                R.TableCreate("Takeovers").OptArg("primary_key", "Name").Run(Conn);
            }
            if (!(bool)R.TableList().Contains("Tickets").Run(Conn)) {
                Console.WriteLine("\"Tickets\" table was not found, so it is being made.");
                R.TableCreate("Tickets").OptArg("primary_key", "ID").Run(Conn);
            }
            if (!(bool)R.TableList().Contains("Twitter").Run(Conn)) {
                Console.WriteLine("\"Twitter\" table was not found, so it is being made.");
                R.TableCreate("Twitter").OptArg("primary_key", "Account ID").Run(Conn);
            }
        }
        /*
         * @todo Adjust collecting resources like config.json and others.
         * @body This needs to be edited later, either the location of all the resources should be a commandline argument, or we simply have a commandline argument named --dev or something to differentiate the environment KekBot is in. That way we can test but still have all the proper resources and stuff where they need to be.
         */
    }
}
