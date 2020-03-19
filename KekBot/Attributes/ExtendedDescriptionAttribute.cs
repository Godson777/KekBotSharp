using System;
using System.Collections.Generic;
using System.Text;

namespace KekBot.Attributes {
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
    class ExtendedDescriptionAttribute : Attribute {
        public ExtendedDescriptionAttribute(string desc) => ExtendedDescription = desc;

        public string ExtendedDescription { get; }
    }
}
