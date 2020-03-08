using System;
using System.Collections.Generic;
using System.Text;

namespace KekBot.Attributes {
    //This entire class exists LITERALLY for optional params to be told they're required in the help command.
    [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false)]
    public class RequiredAttribute : Attribute {
        public RequiredAttribute() { }
    }
}
