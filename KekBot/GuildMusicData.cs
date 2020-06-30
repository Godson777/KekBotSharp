using DSharpPlus.Entities;
using DSharpPlus.Lavalink;
using DSharpPlus.Lavalink.EventArgs;
using ImageMagick.Defines;
using KekBot.Services;
using KekBot.Utils;
using Newtonsoft.Json;
using RethinkDb.Driver.Ast;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using JsonIgnoreAttribute = Newtonsoft.Json.JsonIgnoreAttribute;

namespace KekBot {
    public sealed class GuildMusicData {
        public string Identifier { get; }

        public RepeatMode RepeatMode { get; private set; } = RepeatMode.None;

        public bool IsPlaying { get; private set; } = false;

        public int Volume { get; private set; } = 100;

        public IReadOnlyCollection<MusicItem> Queue { get; }

        public MusicItem NowPlaying { get; private set; } = default;

        public DiscordChannel Channel => this.Player?.Channel;

        public DiscordChannel CommandChannel { get; set; }

        public DiscordMember Host { get; set; }

        public bool IsMeme { get; private set; } = false;

        private List<MusicItem> QueueInternal { get; }
        private SemaphoreSlim QueueInternalLock { get; }
        /*
         * From my understanding, we could use this to save the queue to a database.
         * Might be useful if we want the music player to boot itself back up after a reboot?
         */
        private string QueueSerialized { get; set; }
        private DiscordGuild Guild { get; }
        //rng?
        private LavalinkService Lavalink { get; }
        private LavalinkGuildConnection Player { get; set; }


        public GuildMusicData(DiscordGuild guild, LavalinkService lavalink) {
            this.Guild = guild;
            this.Lavalink = lavalink;
            this.Identifier = this.Guild.Id.ToString(CultureInfo.InvariantCulture);
            this.QueueInternalLock = new SemaphoreSlim(1, 1);
            this.QueueInternal = new List<MusicItem>();
            this.Queue = new ReadOnlyCollection<MusicItem>(this.QueueInternal);
        }

        public async Task PlayAsync() {
            if (this.Player == null || !this.Player.IsConnected) return;

            if (this.NowPlaying.Track?.TrackString == null) await this.PlayHandlerAsync();
        }

        public async Task StopAsync() {
            if (this.Player == null || !this.Player.IsConnected) return;

            this.NowPlaying = default;
            await this.Player.StopAsync();
        }

        public async Task PauseAsync() {
            if (this.Player == null || !this.Player.IsConnected) return;

            this.IsPlaying = false;
            await this.Player.PauseAsync();
        }

        public async Task ResumeAsync() {
            if (this.Player == null || !this.Player.IsConnected)
                return;

            this.IsPlaying = true;
            await this.Player.ResumeAsync();
        }

        public async Task SetVolumeAsync(int volume) {
            if (this.Player == null || !this.Player.IsConnected) return;

            await this.Player.SetVolumeAsync(volume);
            this.Volume = volume;
        }

        public async Task RestartAsync() {
            if (this.Player == null || !this.Player.IsConnected) return;

            if (this.NowPlaying.Track.TrackString == null) return;

            await this.QueueInternalLock.WaitAsync();
            try {
                this.QueueInternal.Insert(0, this.NowPlaying);
                await this.Player.StopAsync();
            } finally {
                this.QueueInternalLock.Release();
            }
        }

        public async Task SeekAsync(TimeSpan target, bool relative) {
            if (this.Player == null || !this.Player.IsConnected) return;

            if (!relative) await this.Player.SeekAsync(target);
            else await this.Player.SeekAsync(this.Player.CurrentState.PlaybackPosition + target);
        }

        public int EmptyQueue() {
            lock (this.QueueInternal) {
                var itemCount = this.QueueInternal.Count;
                this.QueueInternal.Clear();
                return itemCount;
            }
        }

        public void Shuffle() {
            lock (this.QueueInternal) {
                this.QueueInternal.Shuffle();
            }
        }

        public void SetRepeatMode(RepeatMode mode) {
            var pMode = this.RepeatMode;
            this.RepeatMode = mode;
            if (this.NowPlaying.Track.TrackString != null) {
                if (mode == RepeatMode.Single && mode != pMode) {
                    lock (this.QueueInternal) {
                        this.QueueInternal.Insert(0, this.NowPlaying);
                    }
                } else if (mode != RepeatMode.Single && pMode == RepeatMode.Single) {
                    lock (this.QueueInternal) {
                        this.QueueInternal.RemoveAt(0);
                    }
                }
            }
        }

        public void Enqueue(MusicItem item) {
            lock (this.QueueInternal) {
                if (this.RepeatMode == RepeatMode.All && this.QueueInternal.Count == 1) {
                    this.QueueInternal.Insert(0, item);
                } else {
                    this.QueueInternal.Add(item);
                }
            }
        }

        public async Task Skip(int toSkip) {
            if (this.Player == null || !this.Player.IsConnected) return;

            this.NowPlaying = default;
            for (int i = 0; i < toSkip; i++) {
                Dequeue();
            }
            await this.Player.StopAsync();
        }

        public MusicItem? Dequeue() {
            lock (this.QueueInternal) {
                if (this.QueueInternal.Count == 0)
                    return null;

                if (this.RepeatMode == RepeatMode.None) {
                    var item = this.QueueInternal[0];
                    this.QueueInternal.RemoveAt(0);
                    return item;
                }

                if (this.RepeatMode == RepeatMode.Single) {
                    var item = this.QueueInternal[0];
                    return item;
                }

                if (this.RepeatMode == RepeatMode.All) {
                    var item = this.QueueInternal[0];
                    this.QueueInternal.RemoveAt(0);
                    this.QueueInternal.Add(item);
                    return item;
                }
            }

            return null;
        }

        public MusicItem? Remove(int index) {
            lock (this.QueueInternal) {
                if (index < 0 || index >= this.QueueInternal.Count)
                    return null;

                var item = this.QueueInternal[index];
                this.QueueInternal.RemoveAt(index);
                return item;
            }
        }

        public async Task<bool> CreatePlayerAsync(DiscordChannel channel) {
            if (this.Player != null && this.Player.IsConnected)
                return false;

            this.Player = await this.Lavalink.LavalinkNode.ConnectAsync(channel);
            if (this.Volume != 100)
                await this.Player.SetVolumeAsync(this.Volume);
            this.Player.PlaybackFinished += this.PlaybackFinished;
            await CommandChannel.SendMessageAsync("Music Session Started (Debug Message)");
            return true;
        }

        public async Task CreateMemeAsync(DiscordChannel channel) {
            if (this.Player != null && this.Player.IsConnected)
                return;

            this.Player = await this.Lavalink.LavalinkNode.ConnectAsync(channel);
            this.IsMeme = true;
            if (this.Volume != 100)
                await this.Player.SetVolumeAsync(this.Volume);
            this.Player.PlaybackFinished += this.PlaybackFinished;
        }

        public async Task DestroyPlayerAsync() {
            if (this.Player == null)
                return;

            if (this.Player.IsConnected)
                await this.Player.DisconnectAsync();

            this.Player = null;
            this.Host = null;
            await CommandChannel.SendMessageAsync("Music Session Ended (Debug Message)");
            this.CommandChannel = null; 
        }

        public TimeSpan GetCurrentPosition() {
            if (this.NowPlaying.Track.TrackString == null)
                return TimeSpan.Zero;

            return this.Player.CurrentState.PlaybackPosition;
        }

        public TimeSpan GetTimeBeforeNext() {
            lock (this.QueueInternal) {
                if (this.NowPlaying.Track.TrackString == null) return TimeSpan.Zero;

                var time = this.Player.CurrentState.CurrentTrack.Length - this.Player.CurrentState.PlaybackPosition;
                for (int i = 0; i < QueueInternal.Count - 1; i++) {
                    MusicItem track = this.QueueInternal[i];
                    time += track.Track.Length;
                }
                return time;
            }
        }

        private async Task PlaybackFinished(TrackFinishEventArgs e) {
            await Task.Delay(500);
            this.IsPlaying = false;
            await this.PlayHandlerAsync();
        }

        private async Task PlayHandlerAsync() {
            var itemN = this.Dequeue();
            if (itemN == null) {
                await Task.Delay(500);
                await DestroyPlayerAsync();
                return;
            }

            var item = itemN.Value;
            this.NowPlaying = item;
            this.IsPlaying = true;
            await this.Player.PlayAsync(item.Track);
            if (IsMeme) return;
            var builder = new DiscordEmbedBuilder();
            builder.Color = CommandChannel.Guild.CurrentMember.Color;
            builder.AddField("Now Playing:", item.Track.Title, true);
            builder.AddField("Queued By:", item.RequestedBy.Mention, true);
            builder.AddField("URL:", item.Track.Uri.ToString(), false);
            builder.WithThumbnail(item.RequestedBy.AvatarUrl);
            await CommandChannel.SendMessageAsync(embed: builder.Build());
        }
    }

    public struct MusicItem {
        [JsonIgnore]
        public LavalinkTrack Track { get; }

        [JsonIgnore]
        public DiscordMember RequestedBy { get; }

        public MusicItem(LavalinkTrack track, DiscordMember requester) {
            this.Track = track;
            this.RequestedBy = requester;
        }
    }

    public struct MusicItemSerializable {
        [JsonProperty("track")]
        public string Track { get; set; }

        [JsonProperty("member_id")]
        public ulong MemberId { get; set; }

        public MusicItemSerializable(MusicItem mi) {
            this.Track = mi.Track.TrackString;
            this.MemberId = mi.RequestedBy.Id;
        }
    }

    public enum RepeatMode {
        /// <summary>
        /// No Repeat
        /// </summary>
        None,

        /// <summary>
        /// Single Repeat
        /// </summary>
        Single,

        /// <summary>
        /// Entire Queue Repeat
        /// </summary>
        All
    }
}
