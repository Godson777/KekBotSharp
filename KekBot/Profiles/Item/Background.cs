using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace KekBot.Profiles.Item {
    public class Background : Item {
        public override Stream DrawItem() {
            return new FileStream($"../../../../resources/profile/background/{File}", FileMode.Open);
        }

        
    }
}
