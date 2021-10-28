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
using KekBot.Arguments;
using KekBot.ArgumentResolvers;
using KekBot.Commands;
using KekBot.Lib;
using KekBot.Services;
using KekBot.Utils;
using System.Collections;
using System.Reflection;
using DSharpPlus.Interactivity.Extensions;
using Microsoft.Extensions.Logging;
using DSharpPlus.Exceptions;
using DSharpPlus.SlashCommands;

namespace KekBot {
    /// <summary>
    /// Represents a single shard of KekBot
    /// </summary>
    public sealed class KekBot {
        /// <summary>
        /// Info on all commands, "real" and "fake".
        /// </summary>
        static readonly CommandInfoList CommandInfos = new CommandInfoList();

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
        private ConcurrentDictionary<ulong?, string> PrefixSettings { get; }
        private IServiceProvider Services { get; }
        private string DefaultPrefix = "$";

        private readonly object _logLock = new object();

        [SuppressMessage("Globalization", "CA1303:Do not pass literals as localized parameters", Justification = "Running the bot requires knowledge of English")]
        public KekBot(Config config, int shardID) {
            ShardID = shardID;

            Discord = new DiscordClient(new DiscordConfiguration() {
                Token = config.Token,
                TokenType = TokenType.Bot,
                ShardCount = config.Shards,
                ShardId = ShardID,
                Intents = DiscordIntents.AllUnprivileged | DiscordIntents.GuildMembers,

                AutoReconnect = true,
                ReconnectIndefinitely = true,
                GatewayCompressionLevel = GatewayCompressionLevel.Stream,
                LargeThreshold = 1500,

                MinimumLogLevel = LogLevel.Debug
            });

            Discord.GuildAvailable += GuildAvailable;
            Discord.Ready += Ready;
            Discord.ClientErrored += DiscordErrored;
            Discord.SocketErrored += SocketErrored;

            Services = new ServiceCollection()
                .AddSingleton(CommandInfos)
                .AddSingleton(FakeCommands)
                .AddSingleton<MusicService>()
                .AddSingleton(new LavalinkService(this.Discord))
                .AddSingleton(this)
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
            CommandsNext.RegisterUserFriendlyTypeName<ChoicesList>("string[]");
            CommandsNext.RegisterConverter(new FlagsConverter());

            CommandsNext.RegisterCommands<OwnerCommands>();
            CommandsNext.RegisterCommands<HelpCommand>();
            CommandsNext.RegisterCommands<FunCommands>();
            
            // Put your guild ID here if you wanna test
            ulong? testGuildId = 233647821784350722;
            var slash = Discord.UseSlashCommands(new SlashCommandsConfiguration()
            {
                Services = new ServiceCollection()
                    .AddSingleton(new WeebCommandsBase(Name, Version, config.WeebToken))
                    .BuildServiceProvider()
            });
            slash.SlashCommandErrored += async (sender, args) =>
            {
                Console.WriteLine("Slash command error.");
                var e = args.Exception;
                Console.WriteLine(e);
                var errMsg = $"Command failed: {e.Message}";
                var ctx = args.Context;
                try
                {
                    await ctx.FollowUpAsync(
                        new DiscordFollowupMessageBuilder().WithContent(errMsg));
                }
                catch (NotFoundException)
                {
                    await ctx.Channel.SendMessageAsync(errMsg);
                }
            };
            slash.RegisterCommands<PingCommand>(testGuildId);
            slash.RegisterCommands<MemeCommands>(testGuildId);
            slash.RegisterCommands<MemeCommands.VoiceCommands>(testGuildId);

            if (config.WeebToken == null) {
                Discord.Logger.Log(LogLevel.Information, $"[{LOGTAG}-{ShardID}] NOT registering weeb commands because no token was found >:(", DateTime.Now);
            } else {
                Discord.Logger.Log(LogLevel.Information, $"[{LOGTAG}-{ShardID}] Initializing weeb commands", DateTime.Now);
                slash.RegisterCommands<WeebCommands>(testGuildId);
            }

            var modules = CommandsNext.RegisteredCommands.Values
                .Select(cmd => cmd.Module)
                .OfType<DSharpPlus.CommandsNext.Entities.SingletonCommandModule>()
                .Select(mod => mod.Instance)
                .Distinct();
            // Note: this will NOT work for slash commands.
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

            PrefixSettings = new ConcurrentDictionary<ulong?, string>();

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
            CommandInfos.AddRange(CommandsNext.RegisteredCommands.Values.Select(CommandInfo.From));
            CommandInfos.AddRange(CommandsNext.RegisteredCommands.Values.OfType<CommandGroup>().SelectMany(c => c.Children).Select(CommandInfo.From));
            CommandInfos.AddRange(FakeCommands.Values.Distinct().SelectMany(faker => faker.FakeCommandInfo));
        }

        private Task SocketErrored(DiscordClient client, SocketErrorEventArgs e) {
            var ex = e.Exception;
            while (ex is AggregateException)
                ex = ex.InnerException;

            client.Logger.Log(LogLevel.Critical, $"[{LOGTAG}-{ShardID}] Socket threw an exception {ex}");
            return Task.CompletedTask;
        }

        /*[SuppressMessage("Globalization", "CA1303:Do not pass literals as localized parameters", Justification = "shut your whore mouth")]
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
        }*/

        [SuppressMessage("Globalization", "CA1303:Do not pass literals as localized parameters", Justification = "I will destroy you")]
        public async Task StartAsync() {
            Discord.Logger.Log(LogLevel.Information, $"[{LOGTAG}-{ShardID}] Booting KekBot Shard.");
            await Task.WhenAll(ThingsToWaitFor);
            await Discord.ConnectAsync();
        }

        private Task Ready(DiscordClient client, ReadyEventArgs e) {
            client.Logger.Log(LogLevel.Information, $"[{LOGTAG}-{ShardID}] KekBot is ready to roll!");

            if (GameTimer == null) {
                GameTimer = new Timer(GameTimerCallback, client, TimeSpan.Zero, TimeSpan.FromMinutes(15));
            }
            return Task.CompletedTask;
        }

        private async Task GuildAvailable(DiscordClient client, GuildCreateEventArgs e) {
            var set = await LegacySettings.Get(e.Guild);
            if (set.Prefix != null) PrefixSettings.AddOrUpdate(e.Guild.Id, set.Prefix, (k, old) => set.Prefix);
        }

        private Task DiscordErrored(DiscordClient client, ClientErrorEventArgs e) {
            var ex = e.Exception;
            while (ex is AggregateException)
                ex = ex.InnerException;

            client.Logger.Log(LogLevel.Critical, $"[{LOGTAG}-{ShardID}] {e.EventName} threw an exception {ex.GetType()}: {ex.Message}");
            return Task.CompletedTask;
        }

        private Task<int> ResolvePrefixAsync(DiscordMessage msg) {
            var guildId = msg.Channel.GuildId;
            if (guildId == 0) return Task.FromResult(-1);

            var pLen = PrefixSettings.TryGetValue(guildId, out string? prefix) && prefix != null
                ? msg.GetStringPrefixLength(prefix)
                : msg.GetStringPrefixLength(DefaultPrefix);

            return Task.FromResult(pLen);
        }

        private async Task HandleError(CommandsNextExtension cnext, CommandErrorEventArgs errorArgs) {
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

                case BadRequestException e: {
                        ctx.Client.Logger.LogError(e.Errors);
                        return;
                    }
            }

            await ctx.Channel.SendMessageAsync($"An error occured: {error.Message}");
            ctx.Client.Logger.Log(LogLevel.Error, $"[{LOGTAG}-{ShardID}] User '{ctx.User.Username}#{ctx.User.Discriminator}' ({ctx.User.Id}) tried to execute '{errorArgs.Command?.QualifiedName ?? "UNKNOWN COMMAND?"}' " +
                $"but failed with {error}");
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
                client.Logger.Log(LogLevel.Information, $"[{LOGTAG}-{ShardID}] Presense updated to ({type}) {status}");
                GC.Collect();
            } catch (Exception e) {
                client.Logger.Log(LogLevel.Error, $"[{LOGTAG}-{ShardID}] Could not update presense ({e.GetType()}: {e.Message})");
            }
        }

        public string GetPrefix(DiscordGuild guild) {
            PrefixSettings.TryGetValue(guild.Id, out var prefix); 
            return prefix ?? DefaultPrefix;
        }
    }
}
