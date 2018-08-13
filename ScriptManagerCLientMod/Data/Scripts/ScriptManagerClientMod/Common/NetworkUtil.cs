using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox.ModAPI;
using ScriptManager.ClientMod.Requests;

namespace ScriptManager.ClientMod.Common
{
    public static class NetworkUtil
    {
        public static void SendToClient(ulong recipient, RemoteRequest payload)
        {
            var bytes = MyAPIGateway.Utilities.SerializeToBinary(payload);
            MyAPIGateway.Multiplayer.SendMessageTo(Config.MessageHandlerID, bytes, recipient);
            //ReceiveRemoteRequest(bytes);
        }

        public static RemoteRequest DeserializeRequest(byte[] bytes)
        {
            var request = MyAPIGateway.Utilities.SerializeFromBinary<RemoteRequest>(bytes);

            if (request == null)
                throw new Exception("Invalid packet data received!\nCould not parse RemoteRequest");

            return request;
        }
    }
}
