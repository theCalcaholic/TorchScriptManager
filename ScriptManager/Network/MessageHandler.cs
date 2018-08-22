using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Game.ModAPI;
using VRage.Game.Components;
using Sandbox.ModAPI;
using System.Collections.Specialized;
using System.Collections.ObjectModel;
using NLog;
using ScriptManager.ClientMod.Requests;
//using ScriptManager.ClientMod;
//using ScriptManager.ClientMod.Network;
using ScriptManager.ClientMod.Common;
using Sandbox.Game.EntityComponents;
using ScriptManager.Ui;
using System.ComponentModel;

namespace ScriptManager.Network
{
    public static class MessageHandler
    {
        private static readonly Logger Log = LogManager.GetLogger("ScriptManager");
        private static bool initialized = false;
        private static bool messagingReady = false;
        private static Dictionary<long, string> scripts = new Dictionary<long, string>();

        public static void Init()
        {
            if (initialized)
                return;

            ScriptManagerPlugin.Instance.Config.ScriptEntryChanged += OnScriptEntryUpdate;
            ScriptManagerPlugin.Instance.Config.Whitelist.CollectionChanged += UpdateWhitelist;
            foreach (var script in ScriptManagerPlugin.Instance.Config.Whitelist)
                if( script.Enabled )
                    scripts[script.Id] = script.Name;

            initialized = true;

        }

        public static void SetupMessaging()
        {
            MyAPIGateway.Multiplayer.RegisterMessageHandler(Config.MessageHandlerID, ReceiveRemoteRequest);
            //MyAPIGateway.Utilities.RegisterMessageHandler(Config.MOD_ID, ReceiveWhitelist);
            messagingReady = true;
        }
        private static void ReceiveRemoteRequest(byte[] bytes)
        {
            Log.Info("Received remote request...");
            RemoteRequest request = null;
            try
            {
                request = NetworkUtil.DeserializeRequest(bytes);
            }
            catch (Exception e)
            {
                Log.Error(e.Message);
                return;
            }
            Log.Info("Successfully deserialized RemoteRequest.");
            if (request.RequestType == RemoteRequestType.CLIENT_REGISTRATION)
            {
                SendWhitelistToClient(request.Sender);
            }
            else if (request.RequestType == RemoteRequestType.RECOMPILE)
            {
                var recompileRequest = request as RecompileRequest;
                if (recompileRequest == null)
                {
                    Log.Error("No serialized data (expected RecompileRequest)!");
                    return;
                }
                RecompilePB(recompileRequest.PbId, recompileRequest.ScriptId);
            }
            else
            {
                Log.Warn(string.Format("Invalid request type '{0}' to server!", request.RequestType));
            }
        }

        /*private static void ReceiveWhitelist(object data)
        {
            if (!MyAPIGateway.Multiplayer.IsServer)
                return;

            Log.Info("Received whitelist from plugin...");
            var errorMsg = "Received invalid whitelist data (server): {0}!";
            var dataList = data as object[];
            if (dataList == null || dataList.Length != 3)
            {
                Log.Error(errorMsg, string.Format("Expected object[3] (but {0})", (dataList == null ? "cast failed" : "had invalid length '" + dataList.Length + "'")));
                return;
            }
            var actionStr = dataList[0] as string;
            var scriptTitles = dataList[1] as Dictionary<long, string>;
            var scriptBodies = dataList[2] as Dictionary<long, string>;
            if (actionStr == null
                || scriptTitles == null
                || scriptBodies == null)
            {
                Log.Error(errorMsg, "First object needs to be a string and the 2nd and 3rd object need to be of type Dictionary<long, string>");
                return;
            }
            ListUpdateAction action = ListUpdateAction.ADD;
            if (!Enum.TryParse(actionStr, out action))
            {
                Log.Error(errorMsg, "Invalid action string");
                return;
            }

            if (action == ListUpdateAction.ADD)
            {
                Log.Info("Adding scripts to whitelist...");
                foreach (var k in scriptTitles.Keys)
                {
                    Log.Info("    {0}: {1}", k, scriptTitles[k]);
                    if (k != -1)
                    {
                        WhitelistData.Scripts[k] = scriptTitles[k];
                        WhitelistData.ScriptBodies[k] = scriptBodies[k];
                    }
                }
            }
            else if (action == ListUpdateAction.REMOVE)
            {
                Log.Info("Removing scripts to whitelist...");
                foreach (var k in scriptTitles.Keys)
                {
                    Log.Info("    {0}: {1}", k, scriptTitles[k]);
                    if (WhitelistData.Scripts.ContainsKey(k) && k != -1)
                    {
                        WhitelistData.Scripts.Remove(k);
                        WhitelistData.ScriptBodies.Remove(k);
                    }
                }
            }

        }*/

        public static void Broadcast(RemoteRequest payload)
        {
            var bytes = MyAPIGateway.Utilities.SerializeToBinary(payload);
            var players = new List<IMyPlayer>();
            MyAPIGateway.Players.GetPlayers(players);
            foreach (var player in players)
            {
                var id = player.SteamUserId;
                if (id != MyAPIGateway.Multiplayer.MyId && id != MyAPIGateway.Multiplayer.ServerId)
                    NetworkUtil.SendToClient(id, payload);
            }
        }

        public static void SendWhitelistToClient(ulong clientId)
        {
            if (!MyAPIGateway.Multiplayer.IsServer)
                return;

            Log.Info("Sending whitelist to client '{0}'...", clientId);

            var request = new WhitelistActionRequest(
                MyAPIGateway.Multiplayer.MyId,
                ListUpdateAction.ADD,
                scripts);
            NetworkUtil.SendToClient(clientId, request);
        }


        private static void RecompilePB(long pbId, long scriptId)
        {
            if (!MyAPIGateway.Multiplayer.IsServer)
                return;

            var pb = MyAPIGateway.Entities.GetEntityById(pbId) as IMyProgrammableBlock;

            Log.Info(string.Format("Received recompilation request for pb '{0}'", pb.CustomName));

            /*if (pb.Storage == null)
                pb.Storage = new MyModStorageComponent();
            pb.Storage[Config.GUID] = scriptId.ToString();*/

            var script = ScriptManagerPlugin.Instance.Config.Whitelist.FirstOrDefault((item) => item.Id == scriptId);
            if (script == null)
            {
                Log.Error("Invalid script id in recompilation request!");
                return;
            }
            pb.ProgramData = script.Code;
        }

        private static void UpdateWhitelist(object sender, NotifyCollectionChangedEventArgs e)
        {
            if( e.Action == NotifyCollectionChangedAction.Remove || e.Action == NotifyCollectionChangedAction.Replace || e.Action == NotifyCollectionChangedAction.Reset)
            {
                foreach(var item in e.OldItems)
                {
                    var scriptId = (item as ScriptEntry).Id;
                    if ( scripts.ContainsKey(scriptId) )
                    scripts.Remove(scriptId);
                }
            }

            if( e.Action == NotifyCollectionChangedAction.Add || e.Action == NotifyCollectionChangedAction.Replace )
            {
                foreach(var item in e.NewItems)
                {
                    var script = (item as ScriptEntry);
                    scripts[script.Id] = script.Name;
                }
            }

            if(messagingReady)
            {
                ListUpdateAction action = ListUpdateAction.ADD;

                if (e.Action == NotifyCollectionChangedAction.Remove || e.Action == NotifyCollectionChangedAction.Reset)
                    action = ListUpdateAction.REMOVE;

                var request = new WhitelistActionRequest(
                    MyAPIGateway.Multiplayer.MyId,
                    action, scripts);
                Broadcast(request);
            }
        }

        private static void OnScriptEntryUpdate(object sender, PropertyChangedEventArgs e)
        {
            var script = sender as ScriptEntry;
            if (e.PropertyName == nameof(ScriptEntry.Name) && scripts.ContainsKey(script.Id))
                scripts[script.Id] = script.Name;

        }
    }
}
