using System;

namespace KekBot.Attributes {
    /// <summary>
    /// Hide the parameter from the help usage.
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter)]
    internal class HiddenParam : Attribute {
        public HiddenParam() { }
    }
}
