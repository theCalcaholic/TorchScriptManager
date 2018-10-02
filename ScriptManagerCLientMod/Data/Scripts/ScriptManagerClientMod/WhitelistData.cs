using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScriptManager.ClientMod
{
    public class WhitelistData
    {
        public static Dictionary<long, string> Scripts = new Dictionary<long, string>
        {
            { -1, "NONE" }
        };
        public static Dictionary<long, string> ScriptBodies = new Dictionary<long, string>
        {
            { -1, "" }
        };
    }
}
