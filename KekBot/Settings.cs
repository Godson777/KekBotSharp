using DSharpPlus.Entities;
using KekBot.Utils;
using Newtonsoft.Json;
using RethinkDb.Driver.Ast;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace KekBot {
    public class Settings {
        public Settings(string GuildID) {
            this.GuildID = GuildID;
        }

        [JsonProperty("Guild ID")]
        public string GuildID { get; private set; }
        [JsonProperty]
        public string Prefix { get; set; }
        [JsonProperty("AutoRole ID")]
        public string AutoRoleID { get; set; }
        [JsonProperty("Announce Settings")]
        public AnnounceSettings AnnounceSettings { get; set; } = new AnnounceSettings();
        [JsonProperty]
        public ConcurrentDictionary<string, Tag> Tags { get; set; } = new ConcurrentDictionary<string, Tag>();
        [JsonProperty]
        public List<string> Quotes { get; set; } = new List<string>();
        [JsonProperty("Free Roles")]
        public List<string> FreeRoles { get; set; } = new List<string>();
        /*
         * @todo Port over Twitter Feeds from KekBot 1.6.1 BETA to this.
         * @body This is literally here just because I don't feel like adding a public property that isn't going to be used/tested right now. The current properties that need porting over are "Twitter Feeds" and "Twitter Feed Enabled" respectively.
         */
        [JsonProperty("Anti-Ad")]
        public bool AntiAd { get; set; } = false;
        [JsonProperty("Update Channel ID")]
        public string UpdateChannelID { get; set; }
        //Locale? Ehhhhhhhh probs not.

        public static async Task<Settings> Get(string GuildID) => await Program.R.Table("Settings").Get(GuildID).RunAsync<Settings>(Program.Conn) ?? new Settings(GuildID);

        public static async Task<Settings> Get(DiscordGuild Guild) => await Get(Guild.Id.ToString());

        public async Task Save() {
            if (await Program.R.Table("Settings").Get(GuildID).RunAsync<Settings>(Program.Conn) != null) await Program.R.Table("Settings").Get(GuildID).Update(this).RunAsync(Program.Conn);
            else await Program.R.Table("Settings").Insert(this).RunAsync(Program.Conn);
        }
    }

    public struct AnnounceSettings {
        [JsonProperty("welcomeChannelID")]
        public string WelcomeChannelID { get; set; }
        [JsonProperty("welcomeMessage")]
        public string WelcomeMessage { get; set; }
        [JsonProperty("farewellMessage")]
        public string FarewellMessage { get; set; }
    }

    public struct Tag {
        [JsonProperty("name")]
        public string Name;
        [JsonProperty("contents")]
        public string Contents;
        [JsonProperty("creatorID")]
        public string CreatorID;
        [JsonProperty("timeCreated")]
        public string TimeCreated;
        [JsonProperty("timeLastEdited")]
        public string TimeLastEdited;
    }
}
