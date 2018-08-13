using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScriptManagerClientMod
{
    static class CommonData
    {
        public static Dictionary<long, string> Scripts = new Dictionary<long, string>
        {
            { -1, "NONE" }
        };
        public static Dictionary<long, string> ScriptBodies = new Dictionary<long, string>
        {
            { -1, "" }
        };
        public static Guid GUID { get; } = new Guid("5f35403d-2150-4d83-9ef8-3510f25bbdf3");

    }
}
