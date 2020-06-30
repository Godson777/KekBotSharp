using DSharpPlus.Entities;
using KekBot.Profiles.Item;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace KekBot.Profiles {
    public class Profile {
        //private properties
        [JsonProperty("User ID")]
        private string UserID { get; }
        [JsonProperty("Token")]
        private string TokenID { get; }
        [JsonProperty("Background")]
        private List<string> BackgroundID { get; }
        [JsonProperty("Inventory")]
        private List<string> InventoryIDs { get; }
        [JsonProperty("Badge")]
        private string BadgeID { get; }
        [JsonProperty("Next Daily")]
        private long daily { get; set; }

        //public properties

        //public Token Token { get; }
        public Background Background { get; }
        //public Badge Badge { get; }
        [JsonProperty]
        public double Topkeks { get; private set; }
        [JsonProperty]
        public int XP { get; private set; }
        [JsonProperty("Max XP")]
        public int MaxXP { get; private set; }
        [JsonProperty]
        public int Level { get; private set; }
        [JsonProperty]
        public string Subtitle { get; set; } = "Just another user.";
        [JsonProperty]
        public string? Bio { get; set; }
        /*
         * @todo How do we do playlists? Should we just delete this feature entirely?
         * @body I'm thinking we just scrap it entirely, and if we readd it back it can just be in its own table or something. It's not like the feature was used all that much anyway.
         */
        

        private volatile DiscordUser User;

        /*
         * @todo public properties that automatically map out the private `X`ID objects/lists to their appropriate object forms.
         * @body It's as the title implies, once the `ItemRegistry` is done, the `Profile` class will have public properties that just maps out items based on the item registry.
         * Filtering a user's inventory based on item type is simple with LINQ, so this eliminates having to store the user's inventory of tokens and badges seperately.
         * That being said, this introduces a new problem...
         */

        /*
         * @todo Write an `OldProfile` class for the sole purpose of migrating profiles from pre 2.0 builds to post 2.0 builds
         * @body Alternatively, a seperate program could be made in either Java or C# to suit this purpose. 
         * Either way, there needs to be a way to migrate all pre 2.0 profiles to the new format provided in our current `Profile` class.
         */


    }
}
