using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using DSharpPlus.Lavalink;
using KekBot.Attributes;
using KekBot.Menu;
using KekBot.Services;
using KekBot.Utils;
using RethinkDb.Driver.Ast;
using RethinkDb.Driver.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KekBot.Commands {
    [Group("music"), Aliases("m"), Description("Central command for all music related actions.")]
    public sealed class MusicCommand : BaseCommandModule {
        private MusicService Music { get; }
        private YouTubeSearchProvider YouTube { get; }

        private GuildMusicData GuildMusic { get; set; }

        public MusicCommand(MusicService music, YouTubeSearchProvider yt) {
            this.Music = music;
            this.YouTube = yt;
        }
        public override async Task BeforeExecutionAsync(CommandContext ctx) {
            var vs = ctx.Member.VoiceState;
            var chn = vs.Channel;
            if (chn == null) {
                await ctx.RespondAsync($"{DiscordEmoji.FromName(ctx.Client, ":msraisedhand:")} You need to be in a voice channel.");
                throw new CommandCancelledException();
            }

            var mbr = ctx.Guild.CurrentMember?.VoiceState?.Channel;
            if (mbr != null && chn != mbr) {
                await ctx.RespondAsync($"{DiscordEmoji.FromName(ctx.Client, ":msraisedhand:")} You need to be in the same voice channel.");
                throw new CommandCancelledException();
            }

            if (ctx.Command.CustomAttributes.OfType<RequiresMusicHostAttribute>().Any()) {
                if (!ctx.Channel.PermissionsFor(ctx.Member).HasPermission(Permissions.ManageGuild)) {
                    if (this.GuildMusic.Host != null && this.GuildMusic.Host != ctx.Member) {
                        await ctx.RespondAsync("You aren't the host (Debug Message)");
                        throw new CommandCancelledException();
                    }
                }
            }

            this.GuildMusic = await this.Music.GetOrCreateDataAsync(ctx.Guild);
            this.GuildMusic.CommandChannel ??= ctx.Channel;
            this.GuildMusic.Host ??= ctx.Member;

            await base.BeforeExecutionAsync(ctx);
        }

        [Command("queue"), Description("Queues a music track."), Aliases("play", "p", "q"), Priority(1)]
        async Task Play(CommandContext ctx, [Description("URL to play from.")] Uri URL) {
            await Queue(ctx, URL);
        }

        [Command("queue"), Priority(0)]
        async Task Play(CommandContext ctx, [Description("Terms to search for."), RemainingText] string Term) {
            var results = await this.YouTube.SearchAsync(Term);
            if (!results.Any()) {
                await ctx.RespondAsync($"Hm, I can't seem to find {Term} on youtube. Could you try something else?");
                return;
            }

            var menu = new OrderedMenu(ctx.Client.GetInteractivity());
            menu.Users.Add(ctx.User.Id);
            menu.Choices.AddRange(results.Select((x) => $"`{x.Title}` by {x.Author}"));

            menu.Text = "Choose a track:";
            menu.Action = async (_, x) => {
                var el = results.ElementAt(x-1);
                var url = new Uri($"https://youtu.be/{el.Id}");
                await Queue(ctx, url);
            };
            await menu.Display(ctx.Channel);
        }

        async Task Queue(CommandContext ctx, Uri uri) {
            var trackLoad = await this.Music.GetTracksAsync(uri);
            var tracks = trackLoad.Tracks;
            if (trackLoad.LoadResultType == LavalinkLoadResultType.LoadFailed || !tracks.Any()) {
                await ctx.RespondAsync("No tracks were found at specified link.");
                return;
            }

            if (trackLoad.LoadResultType == LavalinkLoadResultType.PlaylistLoaded && trackLoad.PlaylistInfo.SelectedTrack > 0) {
                var index = trackLoad.PlaylistInfo.SelectedTrack;
                tracks = tracks.Skip(index).Concat(tracks.Take(index));
            }

            var trackCount = tracks.Count();
            foreach (var track in tracks)
                this.GuildMusic.Enqueue(new MusicItem(track, ctx.Member));

            var vs = ctx.Member.VoiceState;
            var chn = vs.Channel;
            bool first = await this.GuildMusic.CreatePlayerAsync(chn);
            await this.GuildMusic.PlayAsync();

            if (first) return;
            if (trackCount > 1)
                await ctx.RespondAsync($"Added {trackCount:#,##0} tracks to the queue.");
            else {
                var track = tracks.First();
                await ctx.RespondAsync($"Added {Formatter.InlineCode(track.Title)} by {Formatter.InlineCode(track.Author)} to the queue. (Time before it plays: {Util.PrintTimeSpan(this.GuildMusic.GetTimeBeforeNext())} | {Formatter.Bold($"Queue Position: {this.GuildMusic.Queue.Count}")})");
            }
        }

        [Command("stop"), Description("Stops the current music session. (Host Only)"), Aliases("disconnect", "dc", "leave", "fuckoff", "gtfo"), RequiresMusicHost]
        async Task Stop(CommandContext ctx) {
            this.GuildMusic.EmptyQueue();
            await this.GuildMusic.StopAsync();
            await this.GuildMusic.DestroyPlayerAsync();
        }

        [Command("pause"), Description("Pauses and Unpauses the current music session. (Host Only)"), RequiresMusicHost]
        async Task Pause(CommandContext ctx) {
            if (!this.GuildMusic.IsPlaying) {
                await this.GuildMusic.ResumeAsync();
                await ctx.RespondAsync("Music Resumed.");
            } else {
                await this.GuildMusic.PauseAsync();
                await ctx.RespondAsync("Music Paused.");
            }
        }

        [Command("volume"), Aliases("v", "vol"), Description("Sets the volume to a number you set.")]
        async Task Volume(CommandContext ctx, [Description("Volume to set. Can be 0-150.")] int Volume) {
            if (Volume < 0 || Volume > 150) {
                await ctx.RespondAsync("Specified volume must be between 0 and 150!");
                return;
            }
            await this.GuildMusic.SetVolumeAsync(Volume);
            await ctx.RespondAsync($"Volume set to {Volume}.");
        }

        [Command("repeat"), Description("Toggles repeat mode. Switches from None, Single, and All. (Host Only)"), RequiresMusicHost]
        async Task Repeat(CommandContext ctx) {
            RepeatMode mode = RepeatMode.None;
            switch (this.GuildMusic.RepeatMode) {
                case RepeatMode.None:
                    mode = RepeatMode.Single;
                    break;
                case RepeatMode.Single:
                    mode = RepeatMode.All;
                    break;
                case RepeatMode.All:
                    mode = RepeatMode.None;
                    break;
            }
            this.GuildMusic.SetRepeatMode(mode);
            await ctx.RespondAsync($"Repeat mode is now set to: {Enum.GetName(typeof(RepeatMode), mode)}");
        }

        [Command("skip"), Description("Skips a track. (Or X tracks if specified.) (Host Only)"), RequiresMusicHost]
        async Task Skip(CommandContext ctx, [Description("Number of tracks to skip.")] int ToSkip = 1) {
            var skip = ToSkip - 1;
            if (this.GuildMusic.Queue.Count < skip) {
                await ctx.RespondAsync("There aren't enough tracks to skip!");
                return;
            }

            if (skip > 0) {
                await this.GuildMusic.Skip(skip);
                await ctx.RespondAsync($"Skipped {ToSkip} tracks.");
            } else {
                var track = this.GuildMusic.NowPlaying; 
                await this.GuildMusic.StopAsync();
                await ctx.RespondAsync($"`{track.Track.Title}` by `{track.Track.Author}` has been skipped.");
            }
        }

        [Command("remove"), Description("Removes a track from the queue. (Host Only)"), Aliases("r"), RequiresMusicHost]
        async Task Remove(CommandContext ctx, [Description("The track # to remove.")] int ToRemove) {
            var remove = ToRemove - 1;
            if (this.GuildMusic.Queue.Count < remove) {
                await ctx.RespondAsync($"There isn't a track {ToRemove}!");
                return;
            }

            var track = this.GuildMusic.Remove(remove);
            await ctx.RespondAsync($"Removed `{track?.Track.Title}` by `{track?.Track.Author}` from the queue.");
        }

        [Command("song"), Aliases("nowplaying", "np"), Description("Gets the current song info.")]
        async Task Song(CommandContext ctx) {
            var track = this.GuildMusic.NowPlaying;
            if (track.Track.TrackString == null) return;
            await ctx.RespondAsync($"Now playing: {Formatter.InlineCode(track.Track.Title)} by {Formatter.InlineCode(track.Track.Author)} [{this.GuildMusic.GetCurrentPosition().ToDurationString()}/{this.GuildMusic.NowPlaying.Track.Length.ToDurationString()}] requested by {Formatter.Bold(Formatter.Sanitize(this.GuildMusic.NowPlaying.RequestedBy.DisplayName))}.");
        }

        [Command("playlist"), Aliases("songs"), Description("Shows the list of tracks in the queue.")]
        async Task Playlist(CommandContext ctx) {
            if (this.GuildMusic.RepeatMode == RepeatMode.Single) {
                var track = this.GuildMusic.NowPlaying;
                await (ctx.RespondAsync($"The queue is currently set to repeat: {Formatter.InlineCode(track.Track.Title)} by {Formatter.InlineCode(track.Track.Author)}"));
                return;
            }

            if (this.GuildMusic.Queue.Count == 0) {
                await ctx.RespondAsync("There are no other songs in the queue.");
                return;
            }
            var list = this.GuildMusic.Queue.Select(t => t.ToTrackString()).ToList();
            var builder = new Paginator(ctx.Client.GetInteractivity()) {
                Strings = list
            };
            builder.SetGenericText($"Now Playing: {this.GuildMusic.NowPlaying.ToTrackString()}{(this.GuildMusic.RepeatMode == RepeatMode.All ? " (The entire queue is repeating.)" : "")}");
            builder.Users.Add(ctx.Member.Id);
            builder.FinalAction = async m => {
                await ctx.Message.DeleteAsync();
                await m.DeleteAsync();
            };
            await builder.Display(ctx.Channel);
        }

        [Command("host"), Description("Makes someone else the \"Host\" of the music session."), Aliases("h"), RequiresMusicHost]
        async Task Remove(CommandContext ctx, [Description("The person to make the new host."), RemainingText] DiscordMember NewHost) {
            if (this.GuildMusic.Host == null) return;
            this.GuildMusic.Host = NewHost;
            await ctx.RespondAsync($"Done. `{NewHost.Nickname}` is now the host.");
        }

        //TODO: Seek, Rewind, Foward subcommands.
    }
}
