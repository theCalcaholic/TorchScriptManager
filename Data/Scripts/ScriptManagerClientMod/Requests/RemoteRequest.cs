using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ProtoBuf;

namespace ScriptManagerClientMod
{
    [ProtoContract]
    [ProtoInclude(1, typeof(RecompileRequest))]
    [ProtoInclude(2, typeof(WhitelistActionRequest))]
    public class RemoteRequest
    {

        [ProtoMember]
        public RemoteRequestType RequestType;

        [ProtoMember]
        public ulong Sender = 0;

        public RemoteRequest() { }

        public RemoteRequest(ulong sender, RemoteRequestType type)
        {
            Sender = sender;
            RequestType = type;
        }
    }

    public enum RemoteRequestType : byte { RECOMPILE, WHITELIST_ACTION, CLIENT_REGISTRATION }
}
