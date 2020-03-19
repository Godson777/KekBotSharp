using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using KekBot.Utils;

namespace KekBot {
    internal class CustomEmote {
        //All the json values
        [JsonProperty("thinkings")]
        private List<string> Thinkings;
        [JsonProperty("dances")]
        private List<string> Dances;
        [JsonProperty("topkek")]
        public string Topkek { get; private set; }
        [JsonProperty("gold_trophy")]
        public string GoldTrophy { get; private set; }
        [JsonProperty("silver_trophy")]
        public string SilverTrophy { get; private set; }
        [JsonProperty("bronze_trophy")]
        public string BronzeTrophy { get; private set; }
        [JsonProperty("loadings")]
        private List<string> Loadings;

        //Trying something clever I hope
        private readonly Randumb random = Randumb.Instance;
        public string Think { get {
                return Thinkings[random.Next(Thinkings.Count)];
            } }
        public string Dance { get {
                return Dances[random.Next(Dances.Count)];
            } }
        public string Loading { get {
                return Loadings[random.Next(Loadings.Count)];
            } }

        private static CustomEmote _instance;

        public static async Task<CustomEmote> Get() {
            if (_instance == null) {
                using var fs = File.OpenRead("../../../../config/emotes.json");
                using var sr = new StreamReader(fs, new UTF8Encoding(false));
                return _instance = JsonConvert.DeserializeObject<CustomEmote>(await sr.ReadToEndAsync());
            } else return _instance;
        }
    }
}
