using ImageMagick;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json.Serialization;
using JsonIgnoreAttribute = System.Text.Json.Serialization.JsonIgnoreAttribute;

namespace KekBot.Profiles.Item {
    public abstract class Item {
        /// <summary>
        /// The ID of this item.
        /// </summary>
        [JsonProperty("Item ID")]
        public string ID { get; protected set; }
        /// <summary>
        /// The name of this item.
        /// </summary>
        [JsonProperty("Item Name")]
        public string Name { get; protected set; }
        /// <summary>
        /// The filename of this item.
        /// </summary>
        [JsonProperty("Filename")]
        public string File { get; protected set; }
        /// <summary>
        /// The level required for the user to buy this item.
        /// </summary>
        [JsonProperty("Required Level")]
        public int RequiredLevel { get; protected set; }
        /// <summary>
        /// The price of this item.
        /// </summary>
        public int Price { get; protected set; }
        /// <summary>
        /// The description of this item.
        /// </summary>
        public string Description { get; protected set; } = "Just another item.";
        /// <summary>
        /// Is this item buyable?
        /// </summary>
        public bool Buyable { get; protected set; } = true;
        
        /// <summary>
        /// Draws the item, typically by returning a MagickImage.
        /// </summary>
        /// <returns>The MagickImage containing the drawn item.</returns>
        public abstract MagickImage DrawItem();

        /// <summary>
        /// Equips the item, functionality changes depending on the item.
        /// </summary>
        /// <returns>A bool representing whether or not the equip was successful.</returns>
        public abstract bool Equip(Profile profile);

        /// <summary>
        /// A property to help determine item types from the database.
        /// </summary>
        public abstract string Tag { get; }
    }
}
