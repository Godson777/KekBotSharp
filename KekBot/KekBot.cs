using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
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
        /// Whether the static stuff has been initialized.
        /// </summary>
        private bool IsInitializedStatic = false;

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
        /// Commands that don't exist in CommandsNext's usual system.
        /// </summary>
        readonly FakeCommandsDictionary FakeCommands = new FakeCommandsDictionary();

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
        /// Await all of these tasks in StartAsync to wait for everything in this shard to be initialized.
        /// </summary>
        private readonly List<Task> ThingsToWaitFor = new List<Task>();

        private Timer GameTimer { get; set; } = null;
        private ConcurrentDictionary<ulong, string> PrefixSettings { get; }
        private IServiceProvider Services { get; }

        private readonly object _logLock = new object();

        [SuppressMessage("Globalization", "CA1303:Do not pass literals as localized parameters", Justification = "Running the bot requires knowledge of English")]
        public KekBot(Config config, int shardID) {
            ShardID = shardID;

            Discord = new DiscordClient(new DiscordConfiguration() {
                Token = config.Token,
                TokenType = TokenType.Bot,
                ShardCount = config.Shards,
                ShardId = ShardID,

                AutoReconnect = true,
                ReconnectIndefinitely = true,
                GatewayCompressionLevel = GatewayCompressionLevel.Stream,
                LargeThreshold = 1500,

                UseInternalLogHandler = false,
                LogLevel = LogLevel.Debug
            });

            Services = new ServiceCollection()
                .AddSingleton(CommandInfo)
                .AddSingleton(FakeCommands)
                .AddSingleton(new WeebCommands.WeebCmdsCtorArgs(botName: Name, botVersion: Version, weebToken: config.WeebToken))
                .BuildServiceProvider(true);

            CommandsNext = Discord.UseCommandsNext(new CommandsNextConfiguration {
                CaseSensitive = false,
                IgnoreExtraArguments = true,

                EnableMentionPrefix = true,
                PrefixResolver = ResolvePrefixAsync,

                EnableDefaultHelp = false,
                Services = Services
            });

            CommandsNext.CommandErrored += HandleError;

            CommandsNext.RegisterConverter(new ChoicesConverter());
            CommandsNext.RegisterUserFriendlyTypeName<PickCommand.ChoicesList>("string[]");
            CommandsNext.RegisterConverter(new FlagsConverter());

            CommandsNext.RegisterCommands<TestCommand>();
            CommandsNext.RegisterCommands<PickCommand>();
            CommandsNext.RegisterCommands<PingCommand>();
            CommandsNext.RegisterCommands<OwnerCommands>();
            CommandsNext.RegisterCommands<TestCommandTwo>();
            CommandsNext.RegisterCommands<HelpCommand>();
            CommandsNext.RegisterCommands<FunCommands>();
            CommandsNext.RegisterCommands<QuoteCommand>();

            // TODO: I just realized this would print for every shard. Move this somewhere else?
            if (config.WeebToken == null) {
                Console.WriteLine("NOT registering weeb commands because no token was found >:(");
            } else {
                Console.WriteLine("Initializing weeb commands");
                CommandsNext.RegisterCommands<WeebCommands>();
            }

            var modules = CommandsNext.RegisteredCommands.Values
                .Select(cmd => cmd.Module)
                .OfType<DSharpPlus.CommandsNext.Entities.SingletonCommandModule>()
                .Select(mod => mod.Instance)
                .Distinct();
            foreach (var module in modules) {
                if (module is INeedsInitialized initer) {
                    ThingsToWaitFor.Add(initer.Initialize());
                }
                if (module is IHasFakeCommands faker) {
                    foreach (var name in faker.FakeCommands) {
                        FakeCommands.Add(name, faker);
                    }
                }
            }

            Discord.DebugLogger.LogMessageReceived += DebugLogger_LogMessageReceived;
            Discord.GuildAvailable += GuildAvailable;
            Discord.Ready += Ready;
            Discord.ClientErrored += DiscordErrored;
            Discord.SocketErrored += SocketErrored;

            PrefixSettings = new ConcurrentDictionary<ulong, string>();

            Interactivity = Discord.UseInteractivity(new InteractivityConfiguration());

            Lavalink = Discord.UseLavalink();
        }

        /// <summary>
        /// Initializing this static stuff requires an instance of this class.
        /// </summary>
        [SuppressMessage("Globalization", "CA1303:Do not pass literals as localized parameters", Justification = "shut your whore mouth")]
        public void InitOnce() {
            if (IsInitializedStatic) {
                throw new InvalidOperationException($"The {nameof(KekBot)} class is already initialized!");
            }
            IsInitializedStatic = true;
            CommandInfo.AddRange(CommandsNext.RegisteredCommands.Values.Select(cmd => (ICommandInfo)new CommandInfo(cmd)));
            CommandInfo.AddRange(FakeCommands.Values.Distinct().SelectMany(faker => faker.FakeCommandInfo));
        }

        private Task SocketErrored(SocketErrorEventArgs e) {
            var ex = e.Exception;
            while (ex is AggregateException)
                ex = ex.InnerException;

            e.Client.DebugLogger.LogMessage(LogLevel.Critical, LOGTAG, $"Socket threw an exception {ex}", DateTime.Now);
            return Task.CompletedTask;
        }

        [SuppressMessage("Globalization", "CA1303:Do not pass literals as localized parameters", Justification = "shut your whore mouth")]
        private void DebugLogger_LogMessageReceived(object? sender, DebugLogMessageEventArgs e) {
            lock (_logLock) {
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
                Console.WriteLine(" [{0:00}] {1}", ShardID, e.Message);

                Console.ForegroundColor = fg;
                Console.BackgroundColor = bg;
            }
        }

        [SuppressMessage("Globalization", "CA1303:Do not pass literals as localized parameters", Justification = "I will destroy you")]
        public async Task StartAsync() {
            Discord.DebugLogger.LogMessage(LogLevel.Info, LOGTAG, "Booting KekBot Shard.", DateTime.Now);
            await Task.WhenAll(ThingsToWaitFor);
            await Discord.ConnectAsync();
        }

        private Task Ready(ReadyEventArgs e) {
            e.Client.DebugLogger.LogMessage(LogLevel.Info, LOGTAG, "KekBot is ready to roll!", DateTime.Now);

            if (GameTimer == null) {
                GameTimer = new Timer(GameTimerCallback, e.Client, TimeSpan.Zero, TimeSpan.FromMinutes(15));
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
                        // Remove cuz this swallows important errors >:(
                        break;
                        //var cmd = CommandsNext.FindCommand($"help {errorArgs.Command.QualifiedName}", out var args);
                        //var fakectx = CommandsNext.CreateFakeContext(ctx.Member, ctx.Channel, ctx.Message.Content, ctx.Prefix, cmd, args);
                        //await CommandsNext.ExecuteCommandAsync(fakectx);
                        //return;
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

        private async Task HandleUnknownCommand(CommandContext ctx, string cmdName) {
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
