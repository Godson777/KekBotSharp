using System;

namespace KekBot.Attributes {
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
    public class CategoryAttribute : Attribute {

        public CategoryAttribute(Category category) => Category = category;

        public Category Category { get; }

    }
}
