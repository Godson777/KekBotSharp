using System;

namespace KekBot.Attributes {
    [AttributeUsage(AttributeTargets.Method)]
    partial class CategoryAttribute : Attribute {

        public CategoryAttribute(Category category) => Category = category;

        public Category Category { get; }

    }
}
