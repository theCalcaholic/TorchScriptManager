using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ProtoBuf;

namespace ScriptManagerClientMod
{
    [ProtoContract]
    class RecompileRequest : RemoteRequest
    {
        [ProtoMember(5)]
        public long pbId = 0;

        public RecompileRequest() { }

        public RecompileRequest(ulong sender, long programmableBlock) : base(sender, RemoteRequestType.RECOMPILE)
        {
            pbId = programmableBlock;
        }
    }
}
