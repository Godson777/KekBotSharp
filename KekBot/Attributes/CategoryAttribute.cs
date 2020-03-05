using System;
using System.Collections.Generic;
using System.Text;

namespace KekBot.Attributes {
    partial class CategoryAttribute : Attribute {
        public CategoryAttribute(Category category) {
            this.category = category;
        }

        public Category category { get; }

    }
}
