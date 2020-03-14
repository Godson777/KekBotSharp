using DSharpPlus.Entities;
using KekBot.Utils;
using Newtonsoft.Json;
using RethinkDb.Driver.Ast;
using RethinkDb.Driver.Model;
using RethinkDb.Driver.Net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace KekBot {
    public class LegacySettings {
        public LegacySettings(string GuildID) {
            this.GuildID = GuildID;
        }

        [JsonProperty("Guild ID")]
        public string GuildID { get; private set; }
        [JsonProperty]
        public string Prefix { get; private set; }
        [JsonProperty("AutoRole ID")]
        public string AutoRoleID { get; private set; }
        [JsonProperty("Announce Settings")]
        public AnnounceSettings AnnounceSettings { get; private set; } = new AnnounceSettings();
        [JsonProperty]
        public TagManager Tags { get; private set; } = new TagManager();
        [JsonProperty]
        public QuoteManager Quotes { get; private set; } = new QuoteManager();
        [JsonProperty("Free Roles")]
        public List<string> FreeRoles { get; private set; } = new List<string>();
        /*
         * @todo Port over Twitter Feeds from KekBot 1.6.1 BETA to this.
         * @body This is literally here just because I don't feel like adding a public property that isn't going to be used/tested right now. The current properties that need porting over are "Twitter Feeds" and "Twitter Feed Enabled" respectively.
         */
        [JsonProperty("Anti-Ad")]
        public bool AntiAd { get; private set; } = false;
        [JsonProperty("Update Channel ID")]
        public string UpdateChannelID { get; private set; }
        //Locale? Ehhhhhhhh probs not.

        public static async Task<LegacySettings> Get(string GuildID) {
            var json = await Program.R.Table("LegacySettings").Get(GuildID).ToJson().RunAsync(Program.Conn);
            if (json != null) {
                return JsonConvert.DeserializeObject<LegacySettings>(json);
            } else return new LegacySettings(GuildID);
        }

        public static async Task<LegacySettings> Get(DiscordGuild Guild) => await Get(Guild.Id.ToString());

        public async void Save() {
            var json = JsonConvert.SerializeObject(this);
            await Program.R.Table("LegacySettings").Get(GuildID).Update(Program.R.Json(json)).ToJson().RunAsync(Program.Conn);
        }

        public async static Task Migrate() {
            Cursor<LegacySettings> oldsets = await Program.R.Table("LegacySettings").RunAsync<LegacySettings>(Program.Conn);
            foreach (var oldset in oldsets) {
                var set = new Settings(oldset.GuildID);
                set.Prefix = oldset.Prefix;
                set.AutoRoleID = oldset.AutoRoleID;
                set.AnnounceSettings = oldset.AnnounceSettings;
                if (oldset.Tags != null)
                    foreach (var tag in oldset.Tags?.Tags) {
                        set.Tags.TryAdd(tag.Name, tag);
                    }
                if (oldset.Quotes != null)
                    set.Quotes = oldset.Quotes.Quotes;
                set.FreeRoles = oldset.FreeRoles;
                set.AntiAd = oldset.AntiAd;
                set.UpdateChannelID = oldset.UpdateChannelID;
                await set.Save();
            }

            return;
        }
    }

    public class TagManager {
        [JsonProperty("list")]
        public List<Tag> Tags { get; private set; } = new List<Tag>();

        public bool AddTag(Tag tag) {
            if (!Tags.Any(t => t.Name.Equals(tag.Name))) {
                Tags.Add(tag);
                return true;
            } else return false;
        }

        public Tag GetTagByName(string name) {
            return Tags.SingleOrDefault(t => t.Name == name);
        }

    }

    public class QuoteManager {
        [JsonProperty("list")]
        public List<string> Quotes { get; private set; } = new List<string>();

        /*
         * @todo reuse this code elsewhere
         * @body originally written for KekBot 1.6.1, this method was used to search for quotes in the QuotesManager. That said, this class is obsolete (along with TagManager, which has now been replaced with a ConcurrentDictionary
         */
        /*
       public List<string> Search(string quote) {
           var retList = new List<string>();
           var reg = "(?i).*(" + quote + ").*";

           //The for loop is here to place the quote number in the list, since this gets directly outputted into the command.
           //Otherwise, this could've easily been solved with LINQ.
           for (int i = 0; i < Quotes.Count; i++) {
               if (Regex.IsMatch(Quotes[i], reg)) {
                   retList.Add($"`{i + 1}.` {Quotes[i]}");
               }
           }
           return retList;
       }
       */
    }
}
