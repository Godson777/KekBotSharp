using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace KekBot.Profiles.Item {
    public class Background : Item {
        public override Stream DrawItem() {
            return new FileStream($"Resource/Files/profile/background/{File}", FileMode.Open);
        }

        
    }
}
