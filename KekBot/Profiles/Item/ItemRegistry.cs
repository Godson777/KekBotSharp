using KekBot.Profiles.Item;
using RethinkDb.Driver.Model;
using RethinkDb.Driver.Net;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KekBot.Profiles.Item {
    public class ItemRegistry {
        readonly Dictionary<string, Item> registeredItems;

        private static ItemRegistry? instance;

        public static ItemRegistry Get => instance ??= new ItemRegistry();

        public List<Item> Items => registeredItems.Values.ToList();

        private string[] AcceptableItemTags = { "BG", "Badge", "Token" };

        private ItemRegistry() {
            registeredItems = new Dictionary<string, Item>();
            foreach (string tag in AcceptableItemTags) {
                var list = Program.R.Table("Items").Filter(Program.R.HashMap("Tag", tag)).RunCursor<Background>(Program.Conn);
                foreach (var item in list)
                    RegisterItem(item);
            }
        }

        protected void RegisterItem(Item item) {
            if (item == null) throw new ArgumentNullException("Item", "Can't register a null item.");
            registeredItems.Add(item.ID, item);
        }

        public Item? GetItemByID(string id) {
            registeredItems.TryGetValue(id, out var item);
            return item;
        }

        public async Task AddItem(Item item) {
            if (!AcceptableItemTags.Contains(item.Tag)) {
                throw new ArgumentOutOfRangeException("Item with an invalid tag was attempted to be registered. How did this happen?");
            }

            //When running this check, it's very important not to specify the type of object this is supposed to return.
            //This is because RethinkDB can't instantiate an abstract class during deserialization.
            //So instead, we ask for whatever it is that RethinkDB defaults to returning.
            //The result will still be null if there is no item with such ID, and will not be null if such item exists.
            if (await Program.R.Table("Items").Get(item.ID).RunAsync(Program.Conn) != null) {
                throw new DuplicateNameException("There is already an item with this ID.");
            }

            await Program.R.Table("Items").Insert(item).RunAsync(Program.Conn);
            RegisterItem(item);
        }

        public async Task RemoveItem(Item item) {
            //The same logic applies here as it did in AddItem.
            if (await Program.R.Table("Items").Get(item.ID).RunAsync(Program.Conn) == null) {
                throw new KeyNotFoundException("There is no item with this ID.");
            }

            await Program.R.Table("Items").Insert(item).RunAsync(Program.Conn);
            RegisterItem(item);
        }
    }
}
