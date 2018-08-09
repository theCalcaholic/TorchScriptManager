using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ProtoBuf;

namespace ScriptManagerClientMod
{
    [ProtoContract]
    public class WhitelistActionRequest : RemoteRequest
    {

        [ProtoMember]
        public ListUpdateAction WhitelistAction = ListUpdateAction.ADD;

        [ProtoMember]
        public Dictionary<long, string> Whitelist = new Dictionary<long, string>();

        public WhitelistActionRequest() { }

        public WhitelistActionRequest(
            ulong sender, 
            ListUpdateAction action,
            Dictionary<long, string> whitelist) : base(sender, RemoteRequestType.WHITELIST_ACTION)
        {
            Whitelist = whitelist;
            WhitelistAction = action;
        }
    }

    public enum ListUpdateAction : byte { ADD, REMOVE }
}
