using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace KekBot.Profiles.Item {
    public abstract class Item {
        /// <summary>
        /// The ID of this item.
        /// </summary>
        public string ID { get; }
        /// <summary>
        /// The name of this item.
        /// </summary>
        public string Name { get; }
        /// <summary>
        /// The filename of this item.
        /// </summary>
        public string File { get; }
        /// <summary>
        /// The level required for the user to buy this item.
        /// </summary>
        public int RequiredLevel { get; }
        /// <summary>
        /// The price of this item.
        /// </summary>
        public int Price { get; }
        /// <summary>
        /// The description of this item.
        /// </summary>
        public string Description { get; }
        /// <summary>
        /// Is this item buyable?
        /// </summary>
        public bool Buyable { get; }
        
        /// <summary>
        /// Draws the item, typically by returning a FileStream.
        /// </summary>
        /// <returns>The stream containing the drawn item.</returns>
        public abstract Stream DrawItem();

        /// <summary>
        /// Equips the item, functionality changes depending on the item.
        /// </summary>
        /// <returns>A bool representing whether or not the equip was successful.</returns>
        public abstract bool Equip();
    }
}
