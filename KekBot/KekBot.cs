using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.CommandsNext.Exceptions;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.Interactivity;
using DSharpPlus.Lavalink;
using KekBot.ArgumentResolvers;
using KekBot.Commands;
using KekBot.Lib;
using KekBot.Utils;
using Microsoft.Extensions.DependencyInjection;
using RethinkDb.Driver;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

namespace KekBot {
    /// <summary>
    /// Represents a single shard of KekBot
    /// </summary>
    public sealed class KekBot {
        /// <summary>
        /// Info on all commands, "real" and "fake".
        /// </summary>
        static readonly CommandInfoList CommandInfo = new CommandInfoList();

        /// <summary>
        /// Commands that don't exist in CommandsNext's usual system.
        /// </summary>
        static readonly FakeCommandsDictionary FakeCommands = new FakeCommandsDictionary();

        // TODO: something better.
        private const string Name = "KekBot";
        private const string Version = "2.0";

        /// <summary>
        /// The tag used when emitting log events from the bot.
        /// </summary>
        public const string LOGTAG = "KekBot";

        /// <summary>
        /// Gets the Discord client instance for this shard.
        /// </summary>
        public DiscordClient Discord { get; }

        /// <summary>
        /// Gets the CommandsNext instance.
        /// </summary>
        public CommandsNextExtension CommandsNext { get; }

        /// <summary>
        /// Gets the Interactivity instnace.
        /// </summary>
        public InteractivityExtension Interactivity { get; }

        /// <summary>
        /// Gets the Lavalink instance.
        /// </summary>
        public LavalinkExtension Lavalink { get; }

        /// <summary>
        /// Gets the ID of this shard.
        /// </summary>
        public int ShardID { get; }


        /// <summary>
        /// Await this task to wait for this to be ready to handle weeb.sh requests.
        /// </summary>
        private Task Initialized { get; set; } = Task.CompletedTask;
        private void AddInitTask(Task task) => Initialized = Task.WhenAll(new[] { Initialized, task });

        private Timer GameTimer { get; set; } = null;
        private ConcurrentDictionary<ulong, string> PrefixSettings { get; }
        private IServiceProvider Services { get; }

        private readonly object _logLock = new object();

        [SuppressMessage("Globalization", "CA1303:Do not pass literals as localized parameters", Justification = "Running the bot requires knowledge of English")]
        public KekBot(Config config, int shardID) {
            this.ShardID = shardID;

            this.Discord = new DiscordClient(new DiscordConfiguration() {
                Token = config.Token,
                TokenType = TokenType.Bot,
                ShardCount = config.Shards,
                ShardId = this.ShardID,

                AutoReconnect = true,
                ReconnectIndefinitely = true,
                GatewayCompressionLevel = GatewayCompressionLevel.Stream,
                LargeThreshold = 1500,

                UseInternalLogHandler = false,
                LogLevel = LogLevel.Debug
            });

            this.Services = new ServiceCollection()
                .AddSingleton(FakeCommands)
                .AddSingleton(CommandInfo)
                .BuildServiceProvider(true);

            this.CommandsNext = Discord.UseCommandsNext(new CommandsNextConfiguration {
                CaseSensitive = false,
                IgnoreExtraArguments = true,

                EnableMentionPrefix = true,
                PrefixResolver = ResolvePrefixAsync,

                EnableDefaultHelp = false,
                Services = this.Services
            });

            this.CommandsNext.CommandErrored += HandleError;

            this.CommandsNext.RegisterConverter(new ChoicesConverter());
            this.CommandsNext.RegisterUserFriendlyTypeName<PickCommand.ChoicesList>("string[]");
            this.CommandsNext.RegisterConverter(new FlagsConverter());

            this.CommandsNext.RegisterCommands<TestCommand>();
            this.CommandsNext.RegisterCommands<PickCommand>();
            this.CommandsNext.RegisterCommands<PingCommand>();
            this.CommandsNext.RegisterCommands<OwnerCommands>();
            this.CommandsNext.RegisterCommands<TestCommandTwo>();
            this.CommandsNext.RegisterCommands<HelpCommand>();
            this.CommandsNext.RegisterCommands<FunCommands>();
            this.CommandsNext.RegisterCommands<QuoteCommand>();

            if (config.WeebToken == null) {
                Console.WriteLine("NOT registering weeb commands because no token was found >:(");
            } else {
                Console.WriteLine("Initializing weeb commands");
                var weebCmds = new WeebCommands(botName: Name, botVersion: Version, weebToken: config.WeebToken);
                RegisterFakeCommands(weebCmds);
                // TODO: fix this's docs.
                AddInitTask(weebCmds.Initialized);
            }

            this.Discord.DebugLogger.LogMessageReceived += DebugLogger_LogMessageReceived;
            this.Discord.GuildAvailable += GuildAvailable;
            this.Discord.Ready += Ready;
            this.Discord.ClientErrored += DiscordErrored;
            this.Discord.SocketErrored += SocketErrored;

            this.PrefixSettings = new ConcurrentDictionary<ulong, string>();

            this.Interactivity = Discord.UseInteractivity(new InteractivityConfiguration());

            this.Lavalink = Discord.UseLavalink();
        }

        private Task SocketErrored(SocketErrorEventArgs e) {
            var ex = e.Exception;
            while (ex is AggregateException)
                ex = ex.InnerException;

            e.Client.DebugLogger.LogMessage(LogLevel.Critical, LOGTAG, $"Socket threw an exception {ex}", DateTime.Now);
            return Task.CompletedTask;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Globalization", "CA1303:Do not pass literals as localized parameters", Justification = "shut your whore mouth")]
        private void DebugLogger_LogMessageReceived(object? sender, DebugLogMessageEventArgs e) {
            lock (this._logLock) {
                var fg = Console.ForegroundColor;
                var bg = Console.BackgroundColor;

                Console.ForegroundColor = ConsoleColor.White;
                Console.BackgroundColor = ConsoleColor.Black;
                Console.Write("[{0:yyy-MM-dd HH:mm:ss zzz}] [{1}]", e.Timestamp, e.Application.ToFixedWidth(10));

                switch (e.Level) {
                    case LogLevel.Critical:
                        Console.BackgroundColor = ConsoleColor.Red;
                        Console.ForegroundColor = ConsoleColor.Black;
                        break;

                    case LogLevel.Error:
                        Console.ForegroundColor = ConsoleColor.Red;
                        break;

                    case LogLevel.Warning:
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        break;

                    case LogLevel.Info:
                        Console.ForegroundColor = ConsoleColor.Cyan;
                        break;

                    case LogLevel.Debug:
                        Console.ForegroundColor = ConsoleColor.Magenta;
                        break;
                }
                Console.Write("[{0}]", e.Level.ToString().ToFixedWidth(5));

                Console.ForegroundColor = ConsoleColor.White;
                Console.BackgroundColor = ConsoleColor.Black;
                Console.WriteLine(" [{0:00}] {1}", this.ShardID, e.Message);

                Console.ForegroundColor = fg;
                Console.BackgroundColor = bg;
            }
        }

        public async Task InitOnce(KekBot bot, Config config) {
            CommandInfo.AddRange(bot.CommandsNext.RegisteredCommands.Values.Select(cmd => (ICommandInfo)new CommandInfo(cmd)));
        }

        private static void RegisterFakeCommands(IHasFakeCommands faker) {
            CommandInfo.AddRange(faker.FakeCommandInfo);
            foreach (var name in faker.FakeCommands) {
                FakeCommands.Add(name, faker);
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Globalization", "CA1303:Do not pass literals as localized parameters", Justification = "I will destroy you")]
        public async Task StartAsync() {
            this.Discord.DebugLogger.LogMessage(LogLevel.Info, LOGTAG, "Booting KekBot Shard.", DateTime.Now);
            return this.Discord.ConnectAsync();
        }

        private Task Ready(ReadyEventArgs e) {
            e.Client.DebugLogger.LogMessage(LogLevel.Info, LOGTAG, "KekBot is ready to roll!", DateTime.Now);

            if (this.GameTimer == null) {
                this.GameTimer = new Timer(this.GameTimerCallback, e.Client, TimeSpan.Zero, TimeSpan.FromMinutes(15));
            }
            return Task.CompletedTask;
        }

        private async Task GuildAvailable(GuildCreateEventArgs e) {
            var set = await LegacySettings.Get(e.Guild);
            if (set.Prefix != null) PrefixSettings.AddOrUpdate(e.Guild.Id, set.Prefix, (k, old) => set.Prefix);
        }

        private Task DiscordErrored(ClientErrorEventArgs e) {
            var ex = e.Exception;
            while (ex is AggregateException)
                ex = ex.InnerException;

            e.Client.DebugLogger.LogMessage(LogLevel.Critical, LOGTAG, $"{e.EventName} threw an exception {ex.GetType()}: {ex.Message}", DateTime.Now);
            return Task.CompletedTask;
        }

        private Task<int> ResolvePrefixAsync(DiscordMessage msg) {
            var guildId = msg.Channel.GuildId;
            if (guildId == 0) return Task.FromResult(-1);

            var pLen = PrefixSettings.TryGetValue(guildId, out string? prefix) && prefix != null
                ? msg.GetStringPrefixLength(prefix)
                : msg.GetStringPrefixLength("p$");

            return Task.FromResult(pLen);
        }

        private async Task HandleError(CommandErrorEventArgs errorArgs) {
            var error = errorArgs.Exception;
            var ctx = errorArgs.Context;
            switch (error) {
                case CommandNotFoundException e:
                    await HandleUnknownCommand(ctx, e.CommandName);
                    return;

                case ArgumentException _: {
                        var cmd = CommandsNext.FindCommand($"help {errorArgs.Command.QualifiedName}", out var args);
                        var fakectx = CommandsNext.CreateFakeContext(ctx.Member, ctx.Channel, ctx.Message.Content, ctx.Prefix, cmd, args);
                        await CommandsNext.ExecuteCommandAsync(fakectx);
                        return;
                    }

                case ChecksFailedException e when e.FailedChecks.OfType<RequireUserPermissionsAttribute>().Any(): {
                        var permCheck = e.FailedChecks.OfType<RequireUserPermissionsAttribute>().First();
                        await ctx.RespondAsync($"Woah there, you don't have the `{permCheck.Permissions.ToPermissionString()}` permission! (Temp message)");
                        return;
                    }
            }

            await ctx.Channel.SendMessageAsync($"An error occured: {error.Message}");
            ctx.Client.DebugLogger.LogMessage(LogLevel.Error, LOGTAG, 
                $"User '{ctx.User.Username}#{ctx.User.Discriminator}' ({ctx.User.Id}) tried to execute '{errorArgs.Command?.QualifiedName ?? "UNKNOWN COMMAND?"}' " +
                $"but failed with {error}", DateTime.Now);
        }

        private static async Task HandleUnknownCommand(CommandContext ctx, string cmdName) {
            if (FakeCommands.TryGetValue(cmdName, out var faker)) {
                await faker.HandleFakeCommand(ctx, cmdName);
            }
        }

        private async void GameTimerCallback(object _) {
            var client = _ as DiscordClient;
            try {
                var statuses = await File.ReadAllLinesAsync("Resource/Files/games.txt");
                var status = statuses.RandomElement();
                var type = (ActivityType)Enum.Parse(typeof(ActivityType), status.Substring(0, status.IndexOf(" ")));
                status = status.Substring(status.IndexOf(" ") + 1);

                await client.UpdateStatusAsync(new DiscordActivity(status, type), UserStatus.Online);
                client.DebugLogger.LogMessage(LogLevel.Info, LOGTAG, $"Presense updated to ({type}) {status}", DateTime.Now);
                GC.Collect();
            } catch (Exception e) {
                client.DebugLogger.LogMessage(LogLevel.Error, LOGTAG, $"Could not update presense ({e.GetType()}: {e.Message})", DateTime.Now);
            }
        }
    }
}
