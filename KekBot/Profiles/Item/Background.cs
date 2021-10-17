using ImageMagick;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace KekBot.Profiles.Item {
    public class Background : Item {
        public override string Tag => "BG";

        public override MagickImage DrawItem() {
            return new MagickImage($"Resource/Files/profile/background/{File}");
        }

        /// <summary>
        /// Equips the background to be used in the user's profile.
        /// </summary>
        /// <returns></returns>
        public override bool Equip(Profile profile) {
            //todo actually implement equipping
            return true;
        }

        public static Item New(string id, string name, string file) {
            return new Background() {
                ID = id,
                Name = name,
                File = file,
                Buyable = false
            };
        }
    }
}
