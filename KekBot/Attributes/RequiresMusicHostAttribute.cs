using System;
using System.Collections.Generic;
using System.Text;

namespace KekBot.Attributes {
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    class RequiresMusicHostAttribute : Attribute {
        public RequiresMusicHostAttribute() { }
    }
}
