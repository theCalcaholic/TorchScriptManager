using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ProtoBuf;
using System.ComponentModel;

namespace ScriptManagerClientMod
{
    [ProtoContract]
    class RecompileRequest : RemoteRequest
    {
        [ProtoMember]
        public long PbId = 0;

        [ProtoMember]
        [DefaultValue(-1)]
        public long ScriptId = -1;

        public RecompileRequest() { }

        public RecompileRequest(ulong sender, long programmableBlock, long scriptId) : base(sender, RemoteRequestType.RECOMPILE)
        {
            PbId = programmableBlock;
            ScriptId = scriptId;
        }
    }
}
