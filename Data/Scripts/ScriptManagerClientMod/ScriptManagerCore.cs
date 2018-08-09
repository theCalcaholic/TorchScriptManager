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

namespace ScriptManagerClientMod
{
    [MySessionComponentDescriptor(MyUpdateOrder.NoUpdate)]
    class ScriptManagerCore : MySessionComponentBase
    {
        const ushort ModMessageHandlerId = 36235;
        private long PluginMessageHandlerId = 59300040;
        public static ScriptManagerCore Instance { get; private set; }

        public override void BeforeStart()
        {
            MyAPIGateway.Multiplayer.RegisterMessageHandler(ModMessageHandlerId, ReceiveRemoteRequest);
            if (MyAPIGateway.Multiplayer.IsServer)
            {
                MyAPIGateway.Utilities.RegisterMessageHandler(ModMessageHandlerId, ReceiveWhitelistServer);
                MyAPIGateway.Utilities.SendModMessage(PluginMessageHandlerId, null);
            }
            else
            {
                SendToServer(new RemoteRequest(MyAPIGateway.Multiplayer.MyId,
                    RemoteRequestType.CLIENT_REGISTRATION
                ));
            }

            Instance = this;
        }

        // server only
        private static void ReceiveWhitelistServer(object data)
        {
            if (!MyAPIGateway.Multiplayer.IsServer)
                return;
            
            Logger.Info("Received whitelist from plugin...");
            var errorMsg = "Received invalid whitelist data (server): {0}!";
            var dataList = data as object[];
            if ( dataList == null || dataList.Length != 3)
            {
                Logger.Error(errorMsg, string.Format("Expected object[3] (but {0})", (dataList == null ? "cast failed" : "had invalid length '" + dataList.Length + "'")));
                return;
            }
            var actionStr = dataList[0] as string;
            var scriptTitles = dataList[1] as Dictionary<long, string>;
            var scriptBodies = dataList[2] as Dictionary<long, string>;
            if ( actionStr == null
                || scriptTitles == null
                ||  scriptBodies == null )
            {
                Logger.Error(errorMsg, "First object needs to be a string and the 2nd and 3rd object need to be of type Dictionary<long, string>");
                return;
            }
            ListUpdateAction action = ListUpdateAction.ADD;
            if (!Enum.TryParse(actionStr, out action))
            {
                Logger.Error(errorMsg, "Invalid action string");
                return;
            }

            if(action == ListUpdateAction.ADD )
            {
                Logger.Info("Adding scripts to whitelist...");
                foreach(var k in scriptTitles.Keys )
                {
                    Logger.Info("    {0}: {1}", k, scriptTitles[k]);
                    if (k != -1)
                    {
                        CommonData.Scripts[k] = scriptTitles[k];
                        CommonData.ScriptBodies[k] = scriptBodies[k];
                    }
                }
            }
            else if( action == ListUpdateAction.REMOVE )
            {
                Logger.Info("Removing scripts to whitelist...");
                foreach (var k in scriptTitles.Keys)
                {
                    Logger.Info("    {0}: {1}", k, scriptTitles[k]);
                    if (CommonData.Scripts.ContainsKey(k) && k != -1)
                    {
                        CommonData.Scripts.Remove(k);
                        CommonData.ScriptBodies.Remove(k);
                    }
                }
            }

            var request = new WhitelistActionRequest(
                MyAPIGateway.Multiplayer.MyId,
                action, CommonData.Scripts);
            Broadcast(request);
        }

        // (client only)
        private static void ReceiveWhitelistClient(ListUpdateAction action, object data)
        {

            var receivedScripts = data as Dictionary<long, string>;
            if ( receivedScripts == null)
            {
                Logger.Error("Received invalid whitelist data!");
                return;
            }

            Logger.Info(string.Format("Whitelist received from server ({0} scripts):", receivedScripts.Count));
            foreach (var script in receivedScripts)
            {
                Logger.Info("    " + script.Value);
                CommonData.Scripts[script.Key] = script.Value;
            }
        }

        private static void ReceiveRemoteRequest(byte[] bytes)
        {
            Logger.Info("Received remote request...");

            try
            {
                var request = MyAPIGateway.Utilities.SerializeFromBinary<RemoteRequest>(bytes);
                Logger.Info("Successfully deserialized RemoteRequest.");
                // TODO: Remove check for request type
                if (MyAPIGateway.Multiplayer.IsServer)
                {
                    if ( request.RequestType == RemoteRequestType.CLIENT_REGISTRATION)
                    {
                        SendWhitelistToClient(request.Sender);
                    }
                    else if( request.RequestType == RemoteRequestType.RECOMPILE)
                    {
                        var recompileRequest = request as RecompileRequest;
                        if (recompileRequest == null)
                        {
                            Logger.Error("No serialized data (expected RecompileRequest)!");
                            return;
                        }
                        RecompilePB(recompileRequest.PbId, recompileRequest.ScriptId);
                    }
                    else
                    {
                        Logger.Warning(string.Format("Invalid request type '{0}' to server!", request.RequestType));
                    }
                     

                }
                else
                {
                    if (request.RequestType == RemoteRequestType.WHITELIST_ACTION)
                    {
                        var clientRequest = request as WhitelistActionRequest;
                        if (clientRequest == null)
                        {
                            Logger.Error("No serialized data (expected WhitelistActionRequest)!");
                        }

                        ReceiveWhitelistClient(clientRequest.WhitelistAction, clientRequest.Whitelist);
                    }
                    else
                    {
                        Logger.Warning(string.Format("Invalid request type '{0}' to client!", request.RequestType));
                    }
                }
            }
            catch (Exception e)
            {
                Logger.Error("Invalid packet data!\n {0}", e);
            }
        }

        // (client only)
        public static void SendToServer(RemoteRequest payload)
        {
            var bytes = MyAPIGateway.Utilities.SerializeToBinary(payload);
            MyAPIGateway.Multiplayer.SendMessageToServer(ModMessageHandlerId, bytes);
        }

        // server only
        private static void RecompilePB(long pbId, long scriptId)
        {
            if (!MyAPIGateway.Multiplayer.IsServer)
                return;

            var pb = MyAPIGateway.Entities.GetEntityById(pbId) as IMyProgrammableBlock;

            Logger.Info(string.Format("Received recompilation request for pb '{0}'", pb.CustomName));

            ScriptManagerGameLogic.SetActiveScript(pb, scriptId);
            if(!CommonData.ScriptBodies.ContainsKey(scriptId))
            {
                Logger.Error("Invalid script id in recompilation request!");
                return;
            }
            pb.ProgramData = CommonData.ScriptBodies[scriptId];
        }

        // server only
        public static void SendWhitelistToClient(ulong clientId)
        {
            if (!MyAPIGateway.Multiplayer.IsServer)
                return;

            Logger.Info("Sending whitelist to client '{0}'...", clientId);

            var request = new WhitelistActionRequest(
                MyAPIGateway.Multiplayer.MyId,
                ListUpdateAction.ADD,
                CommonData.Scripts);
            SendToClient(clientId, request);
        }

        // client only
        public static void RequestPBRecompile(IMyProgrammableBlock pb, long scriptId)
        {
            Logger.Info(string.Format("Requesting recompilation of {0} with scriptId {1}", pb.CustomName, scriptId));

            var request = new RecompileRequest(
                MyAPIGateway.Multiplayer.MyId,
                pb.EntityId,
                scriptId);
            SendToServer(request);
        }

        public static void SendToClient(ulong recipient, RemoteRequest payload)
        {
            var bytes = MyAPIGateway.Utilities.SerializeToBinary(payload);
            MyAPIGateway.Multiplayer.SendMessageTo(ModMessageHandlerId, bytes, recipient);
            //ReceiveRemoteRequest(bytes);
        }

        // server only
        public static void Broadcast(RemoteRequest payload)
        {
            var bytes = MyAPIGateway.Utilities.SerializeToBinary(payload);
            var players = new List<IMyPlayer>();
            MyAPIGateway.Players.GetPlayers(players);
            foreach( var player in players)
            {
                var id = player.SteamUserId;
                if (id != MyAPIGateway.Multiplayer.MyId && id != MyAPIGateway.Multiplayer.ServerId)
                    SendToClient(id, payload);
            }
        }
    }


}
