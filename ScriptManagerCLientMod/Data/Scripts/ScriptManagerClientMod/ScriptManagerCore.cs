using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox.Game;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces.Terminal;
using VRage.Game.Components;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Engine.Multiplayer;
using VRage.ModAPI;
using VRage.Game;
using VRage.Game.ModAPI;
using VRage.Utils;
using VRage.Network;
using VRage.Serialization;
using VRage.Library.Collections;
using ScriptManager.ClientMod.Requests;
using ScriptManager.ClientMod.Common;

namespace ScriptManager.ClientMod
{
    [MySessionComponentDescriptor(MyUpdateOrder.NoUpdate)]
    class ScriptManagerCore : MySessionComponentBase
    {
        //public static ScriptManagerCore Instance { get; private set; }

        public override void BeforeStart()
        {
            MyAPIGateway.Multiplayer.RegisterMessageHandler(Config.MessageHandlerID, ReceiveRemoteRequest);
            SendToServer(new RemoteRequest(MyAPIGateway.Multiplayer.MyId,
                RemoteRequestType.CLIENT_REGISTRATION
            ));
        }

        // (client only)
        private static void ReceiveWhitelist(ListUpdateAction action, object data)
        {

            var receivedScripts = data as Dictionary<long, string>;
            if ( receivedScripts == null)
            {
                ModLogger.Error("Received invalid whitelist data!");
                return;
            }

            ModLogger.Info(string.Format("Whitelist received from server ({0} scripts):", receivedScripts.Count));
            foreach (var script in receivedScripts)
            {
                ModLogger.Info("    " + script.Value);
                WhitelistData.Scripts[script.Key] = script.Value;
            }
        }

        private static void ReceiveRemoteRequest(byte[] bytes)
        {
            ModLogger.Info("Received remote request...");
            RemoteRequest request = null;
            try
            {
                request = NetworkUtil.DeserializeRequest(bytes);
            }
            catch( Exception e)
            {
                ModLogger.Error(e.Message);
                return;
            }
            ModLogger.Info("Successfully deserialized RemoteRequest.");
            if (request.RequestType == RemoteRequestType.WHITELIST_ACTION)
            {
                var clientRequest = request as WhitelistActionRequest;
                if (clientRequest == null)
                {
                    ModLogger.Error("No serialized data (expected WhitelistActionRequest)!");
                    return;
                }

                ReceiveWhitelist(clientRequest.WhitelistAction, clientRequest.Whitelist);
            }
            else
            {
                ModLogger.Warning(string.Format("Invalid request type '{0}' to client!", request.RequestType));
            }
        }

        // (client only)
        public static void SendToServer(RemoteRequest payload)
        {
            var bytes = MyAPIGateway.Utilities.SerializeToBinary(payload);
            MyAPIGateway.Multiplayer.SendMessageToServer(Config.MessageHandlerID, bytes);
        }

        // client only
        public static void RequestPBRecompile(IMyProgrammableBlock pb, long scriptId)
        {
            ModLogger.Info(string.Format("Requesting recompilation of {0} with scriptId {1}", pb.CustomName, scriptId));

            var request = new RecompileRequest(
                MyAPIGateway.Multiplayer.MyId,
                pb.EntityId,
                scriptId);
            SendToServer(request);
        }

        /*public static void SendToClient(ulong recipient, RemoteRequest payload)
        {
            var bytes = MyAPIGateway.Utilities.SerializeToBinary(payload);
            MyAPIGateway.Multiplayer.SendMessageTo(Config.MessageHandlerID, bytes, recipient);
            //ReceiveRemoteRequest(bytes);
        }*/
    }


}
