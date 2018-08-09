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
        const ushort ScriptManagerMessageHandlerId = 36235;
        public static ScriptManagerCore Instance { get; private set; }

        public override void BeforeStart()
        {
            MyAPIGateway.Multiplayer.RegisterMessageHandler(ScriptManagerMessageHandlerId, ReceiveRemoteRequest);
            if (MyAPIGateway.Multiplayer.IsServer)
            {
                MyAPIGateway.Utilities.RegisterMessageHandler(ScriptManagerMessageHandlerId, ReceiveWhitelistServer);
                ReceiveWhitelistServer(null);
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
            // TODO: Replace dummy data by data received from plugin

            CommonData.ScriptBodies.Add(100, "public Program { Echo(\"HEYHO\"); }");
            CommonData.Scripts.Add(100, "Greeter");
            return;
            Broadcast(new WhitelistActionRequest(
                MyAPIGateway.Multiplayer.MyId,
                ListUpdateAction.ADD,
                CommonData.Scripts));

            return;

            if (!MyAPIGateway.Multiplayer.IsServer)
                return;


            
            MyLog.Default.Info("Received whitelist from plugin...");

            var errorMsg = "Received invalid whitelist data (server): {0}!";
            var dataList = data as object[];
            if ( dataList == null)
            {
                MyLog.Default.Error(errorMsg, "Expected list of objects");
                return;
            }
            if (dataList.Length != 3)
            {
                MyLog.Default.Error(errorMsg, "Expected object list of length 3");
                return;
            }
            var actionStr = dataList[0] as string;
            var scriptTitles = dataList[1] as Dictionary<long, string>;
            var scriptBodies = dataList[2] as Dictionary<long, string>;
            if ( actionStr == null
                || scriptTitles == null
                ||  scriptBodies == null )
            {
                MyLog.Default.Error(errorMsg, "First object needs to be a string and the 2nd and 3rd object need to be Dictionary<long, string>");
                return;
            }
            ListUpdateAction action = ListUpdateAction.ADD;
            if (!Enum.TryParse(actionStr, out action))
            {
                MyLog.Default.Error(errorMsg, "Invalid action string");
                return;
            }

            if(action == ListUpdateAction.ADD )
            {
                foreach(var k in scriptTitles.Keys )
                {
                    if (CommonData.ScriptBodies.ContainsKey(k) && k != -1)
                    {
                        CommonData.Scripts[k] = scriptTitles[k];
                        CommonData.ScriptBodies[k] = scriptBodies[k];
                    }
                }
            }
            else if( action == ListUpdateAction.REMOVE )
            {
                foreach (var k in scriptTitles.Keys)
                {
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
                MyLog.Default.Error("Received invalid whitelist data!");
                return;
            }

            MyLog.Default.Info(string.Format("Whitelist received from server ({0} scripts):", receivedScripts.Count));
            foreach (var script in receivedScripts)
            {
                MyLog.Default.Info("    " + script.Value);
                CommonData.Scripts[script.Key] = script.Value;
            }
        }

        private static void ReceiveRemoteRequest(byte[] bytes)
        {
            MyLog.Default.Info("Received remote request...");

            try
            {
                var request = MyAPIGateway.Utilities.SerializeFromBinary<RemoteRequest>(bytes);
                MyLog.Default.Info("Successfully deserialized RemoteRequest.");
                // TODO: Remove check for request type
                if (MyAPIGateway.Multiplayer.IsServer)
                {
                    if ( request.RequestType == RemoteRequestType.CLIENT_REGISTRATION)
                    {
                        SendWhitelistToClient(request.Sender);
                    }
                    else if( request.RequestType == RemoteRequestType.RECOMPILE)
                    {
                        var serverRequest = request as RecompileRequest;
                        if (serverRequest == null)
                        {
                            MyLog.Default.Error("No serialized data (expected RecompileRequest)!");
                            return;
                        }
                        RecompilePB(serverRequest.pbId);
                    }
                    else
                    {
                        MyLog.Default.Warning(string.Format("Invalid request type '{0}' to server!", request.RequestType));
                    }
                     

                }
                else
                {
                    if (request.RequestType == RemoteRequestType.WHITELIST_ACTION)
                    {
                        var clientRequest = request as WhitelistActionRequest;
                        if (clientRequest == null)
                        {
                            MyLog.Default.Error("No serialized data (expected WhitelistActionRequest)!");
                        }

                        ReceiveWhitelistClient(clientRequest.WhitelistAction, clientRequest.Whitelist);
                    }
                    else
                    {
                        MyLog.Default.Warning(string.Format("Invalid request type '{0}' to client!", request.RequestType));
                    }
                }
            }
            catch (Exception e)
            {
                MyLog.Default.Error("Invalid packet data!\n {0}", e);
            }
        }

        // (client only)
        public static void SendToServer(RemoteRequest payload)
        {
            var bytes = MyAPIGateway.Utilities.SerializeToBinary(payload);
            MyAPIGateway.Multiplayer.SendMessageToServer(ScriptManagerMessageHandlerId, bytes);
        }

        // server only
        private static void RecompilePB(long pbId)
        {
            if (!MyAPIGateway.Multiplayer.IsServer)
                return;

            var pb = MyAPIGateway.Entities.GetEntityById(pbId) as IMyProgrammableBlock;

            MyLog.Default.Info(string.Format("Received recompile request fro pb {0}", pb.CustomName));

            var scriptId = ScriptManagerGameLogic.GetActiveScript(pb);
            if(!CommonData.ScriptBodies.ContainsKey(scriptId))
            {
                MyLog.Default.Error("Invalid script id in recompilation request!");
                return;
            }
            pb.ProgramData = CommonData.ScriptBodies[scriptId];
        }

        // server only
        public static void SendWhitelistToClient(ulong clientId)
        {
            if (!MyAPIGateway.Multiplayer.IsServer)
                return;

            MyLog.Default.Info("Sending whitelist to client...");

            var request = new WhitelistActionRequest(
                MyAPIGateway.Multiplayer.MyId,
                ListUpdateAction.ADD,
                CommonData.Scripts);
            SendToClient(clientId, request);
        }

        // client only
        public static void RequestPBRecompile(IMyProgrammableBlock pb)
        {
            MyLog.Default.Info(string.Format("Requesting recompilation of {0}", pb.CustomName));

            var request = new RecompileRequest(
                MyAPIGateway.Multiplayer.MyId,
                pb.EntityId);
            SendToServer(request);
        }

        public static void SendToClient(ulong recipient, RemoteRequest payload)
        {
            var bytes = MyAPIGateway.Utilities.SerializeToBinary(payload);
            MyAPIGateway.Multiplayer.SendMessageTo(ScriptManagerMessageHandlerId, bytes, recipient);
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
