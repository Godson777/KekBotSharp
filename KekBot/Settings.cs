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
        private string GuildID;
        /// <summary>
        /// The prefix KekBot will respond to for this server.
        /// </summary>
        [JsonProperty]
        public string Prefix { get; set; }
        /// <summary>
        /// The ID of the role KekBot will give to new members.
        /// </summary>
        [JsonProperty("AutoRole ID")]
        public string AutoRoleID { get; set; }
        /// <summary>
        /// Settings containing the Welcome Channel, and Welcome/Farewell Messages.
        /// </summary>
        [JsonProperty("Announce Settings")]
        public AnnounceSettings AnnounceSettings { get; set; } = new AnnounceSettings();
        /// <summary>
        /// The list of tags belonging to this server.
        /// </summary>
        [JsonProperty]
        public ConcurrentDictionary<string, Tag> Tags { get; set; } = new ConcurrentDictionary<string, Tag>();
        /// <summary>
        /// The list of quotes belonging to this server.
        /// </summary>
        [JsonProperty]
        public List<string> Quotes { get; set; } = new List<string>();
        [JsonProperty("Free Roles")]
        public List<string> FreeRoles { get; set; } = new List<string>();
        /*
         * @todo Port over Twitter Feeds from KekBot 1.6.1 BETA to this.
         * @body This is literally here just because I don't feel like adding a public property that isn't going to be used/tested right now. The current properties that need porting over are "Twitter Feeds" and "Twitter Feed Enabled" respectively.
         */
        /// <summary>
        /// Represents whether or not KekBot will catch unsolicited invites to other guilds and nuke them.
        /// </summary>
        [JsonProperty("Anti-Ad")]
        public bool AntiAd { get; set; } = false;
        /// <summary>
        /// The ID of the channel KekBot will post announcements about updates to.
        /// </summary>
        [JsonProperty("Update Channel ID")]
        public string UpdateChannelID { get; set; }
        //Locale? Ehhhhhhhh probs not.

        /// <summary>
        /// Returns the settings of a specified guild.
        /// </summary>
        /// <param name="GuildID">The ID of the guild to get settings for.</param>
        /// <returns></returns>
        public static async Task<Settings> Get(string GuildID) => await Program.R.Table("Settings").Get(GuildID).RunAsync<Settings>(Program.Conn) ?? new Settings(GuildID);

        /// <summary>
        /// Returns the settings of a specified guild.
        /// </summary>
        /// <param name="Guild">The guild to get settings for.</param>
        public static async Task<Settings> Get(DiscordGuild Guild) => await Get(Guild.Id.ToString());

        /// <summary>
        /// Saves the settings back into the db.
        /// </summary>
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
