using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace KekBot.Profiles.Item {
    public abstract class Item {
        public string ID { get; }
        public string Name { get; }
        public string File { get; }
        public int RequiredLevel { get; }
        public int Price { get; }
        public string Description { get; }
        public bool Buyable { get; }

        public abstract Stream DrawItem();

        /*
         * @todo Mayyyyyybe a Equip() method to equip certain items?
         * @body The method would be abstract, but would make it easier to equip specific items (like setting a user's current token for minigames or their active background) instead of
         * shoving it all into the `Profile` class like it was in the Java version.
         */
    }
}
