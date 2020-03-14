using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
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
using KekBot.Lib;
using DSharpPlus.CommandsNext.Builders;
using DSharpPlus.EventArgs;

namespace KekBot {
    class Program {

        // TODO: do something better with version
        private const string Name = "KekBot";
        private const string Version = "2.0";

        static DiscordClient? Discord;
        static CommandsNextExtension? CNext;
        static FakeCommandsDictionary FakeCommands = new FakeCommandsDictionary();
        static CommandInfoList CommandInfo = new CommandInfoList();
        static readonly ConcurrentDictionary<ulong, string> PrefixSettings = new ConcurrentDictionary<ulong, string>();
        static InteractivityExtension? Interactivity;
        public static RethinkDB R = RethinkDB.R;
        public static Connection Conn;


        static void Main(string[] args) {
            MainAsync(args).ConfigureAwait(false).GetAwaiter().GetResult();
        }

        [SuppressMessage("Globalization", "CA1303:Do not pass literals as localized parameters", Justification = "Running the bot requires knowledge of English")]
        static async Task MainAsync(string[] _) {
            var config = await Config.Get();

            try {
                Conn = R.Connection().User(config.DbUser, config.DbPass).Connect();
            } catch (Exception e) when (e is ReqlDriverError || e is System.Net.Sockets.SocketException) {
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
            VerifyTables();

            Discord = new DiscordClient(new DiscordConfiguration {
                Token = config.Token,
                TokenType = TokenType.Bot,
                LogLevel = LogLevel.Debug,
                UseInternalLogHandler = true
            });

            Interactivity = Discord.UseInteractivity(new InteractivityConfiguration());

            var deps = new ServiceCollection()
                .AddSingleton(FakeCommands)
                .AddSingleton(CommandInfo)
                .BuildServiceProvider();

            CNext = Discord.UseCommandsNext(new CommandsNextConfiguration {
                EnableMentionPrefix = true,
                PrefixResolver = ResolvePrefixAsync,
                IgnoreExtraArguments = true,
                EnableDefaultHelp = false,
                Services = deps
            });

            CNext.CommandErrored += HandleError;
            Discord.GuildAvailable += GuildAvailable;

            CNext.RegisterConverter(new ChoicesConverter());
            CNext.RegisterUserFriendlyTypeName<PickCommand.ChoicesList>("string[]");
            CNext.RegisterConverter(new FlagsConverter());

            CNext.RegisterCommands<TestCommand>();
            CNext.RegisterCommands<PickCommand>();
            CNext.RegisterCommands<PingCommand>();
            CNext.RegisterCommands<OwnerCommands>();
            CNext.RegisterCommands<TestCommandTwo>();
            CNext.RegisterCommands<HelpCommand>();
            CNext.RegisterCommands<FunCommands>();

            if (config.WeebToken == null) {
                Console.WriteLine("NOT registering weeb commands because no token was found >:(");
            } else {
                Console.WriteLine("Initializing weeb commands");
                RegisterFakeCommands(new WeebCommands());
                await WeebCommands.InitializeAsync(name: Name, version: Version, token: config.WeebToken);
            }

            CommandInfo.AddRange(CNext.RegisteredCommands.Values.Select(cmd => (ICommandInfo)new CommandInfo(cmd)));

            await Discord.ConnectAsync();
            await Task.Delay(-1);
        }

        private static void RegisterFakeCommands(IHasFakeCommands faker) {
            CommandInfo.AddRange(faker.FakeCommandInfo);
            foreach (var name in faker.FakeCommands) {
                FakeCommands.Add(name, faker);
            }
        }

        private async static Task HandleError(CommandErrorEventArgs errorArgs) {
            var error = errorArgs.Exception;
            switch (error) {
                case CommandNotFoundException e:
                    await HandleUnknownCommand(errorArgs.Context, e.CommandName);
                    return;

                case ArgumentException e: {
                        var cmd = CNext!.FindCommand($"help {errorArgs.Command.QualifiedName}", out var args);
                        var ctx = errorArgs.Context;
                        var fakectx = CNext.CreateFakeContext(ctx.Member, ctx.Channel, ctx.Message.Content, ctx.Prefix, cmd, args);
                        await CNext.ExecuteCommandAsync(fakectx);
                        return;
                    }
            }

            await errorArgs.Context.Channel.SendMessageAsync($"An error occurred: {error.Message}");
            Console.Error.WriteLine(error);
        }

        private static async Task HandleUnknownCommand(CommandContext ctx, string cmdName) {
            if (FakeCommands.TryGetValue(cmdName, out var faker)) {
                await faker.HandleFakeCommand(ctx, cmdName);
            }
        }

        private async static Task GuildAvailable(GuildCreateEventArgs e) {
            var set = await LegacySettings.Get(e.Guild);
            if (set.Prefix != null) PrefixSettings.AddOrUpdate(e.Guild.Id, set.Prefix, (k, old) => set.Prefix);
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
        [SuppressMessage("Globalization", "CA1303:Do not pass literals as localized parameters", Justification = "Yeah yeah, I'll make a proper logger later.")]
        private static void VerifyTables() {
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
