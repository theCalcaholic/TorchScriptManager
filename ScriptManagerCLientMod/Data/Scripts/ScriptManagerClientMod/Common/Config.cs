using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScriptManager.ClientMod.Common
{
    public static class Config
    {
        public static Guid GUID { get; } = new Guid("5f35403d-2150-4d83-9ef8-3510f25bbdf3");

        public const ushort MessageHandlerID = 44595;

        public static KeyValuePair<long, string> NOSCRIPT { get; } =
            new KeyValuePair<long, string>(-1L, "NONE");
    }
}
