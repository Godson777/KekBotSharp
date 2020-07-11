using DSharpPlus.Entities;
using DSharpPlus.Lavalink;
using KekBot.Services;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace KekBot {
    public sealed class MusicService {
        private LavalinkService Lavalink { get; }
        private ConcurrentDictionary<ulong, GuildMusicData> MusicData { get; }
        private string PlayingQueues { get; set; }


        public MusicService(LavalinkService lavalink) {
            this.Lavalink = lavalink;
            this.MusicData = new ConcurrentDictionary<ulong, GuildMusicData>();
        }

        public async Task<GuildMusicData> GetOrCreateDataAsync(DiscordGuild guild) {
            if (this.MusicData.TryGetValue(guild.Id, out var gmd)) return gmd;

            gmd = this.MusicData.AddOrUpdate(guild.Id, new GuildMusicData(guild, this.Lavalink), (k, v) => v);

            return gmd;
        }

        //Nasty workaround for something, ignore pls.
        public async Task<GuildMusicData> GetDataAsync(DiscordGuild guild) {
            if (this.MusicData.TryGetValue(guild.Id, out var gmd)) return gmd;
            else return null;
        }

        public Task<LavalinkLoadResult> GetTracksAsync(Uri uri) => this.Lavalink.LavalinkNode.Rest.GetTracksAsync(uri);

        public Task<LavalinkLoadResult> GetTracksFromFileAsync(FileInfo file) => this.Lavalink.LavalinkNode.Rest.GetTracksAsync(file);
    }
}
