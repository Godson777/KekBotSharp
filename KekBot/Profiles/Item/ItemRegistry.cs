using KekBot.Profiles.Item;
using System;
using System.Collections.Generic;
using System.Text;

namespace KekBot.Profiles.Item {
    public class ItemRegistry {
        Dictionary<string, Item> registeredItems;

        private ItemRegistry instance;

        public ItemRegistry Get => instance ??= new ItemRegistry();
    }
}
