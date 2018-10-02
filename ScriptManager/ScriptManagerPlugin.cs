using System;
using System.IO;
using System.IO.Compression;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using System.Runtime.Serialization;

using System.Windows.Controls;

using NLog;
using Torch;
using Torch.Managers.PatchManager;
using Torch.Views;
using Torch.API;
using Torch.API.Session;
using Torch.API.Managers;
using Torch.API.Plugins;
using Torch.Session;
using Torch.API.Event;

using Sandbox.ModAPI.Interfaces;
using VRage.Game.Components;
using VRage.Game.ObjectBuilders.Definitions;
using VRage.Compiler;
using VRage.Game;
using VRage.Game.ModAPI;
using VRage.Scripting;
using VRage.Utils;
using VRage.Network;
using VRageMath;
using VRage.Collections;
//using Sandbox.ModAPI.Ingame;
using Sandbox.Game.Entities;
using Sandbox.Common.ObjectBuilders;
using VRage.Game.ModAPI.Ingame;
using Sandbox.Game.EntityComponents;
using Sandbox.Game;
using SpaceEngineers.Game.ModAPI.Ingame;
using Sandbox.Game.Entities.Blocks;
using Sandbox.Engine.Multiplayer;
using System.Security.Cryptography;
using Sandbox.Engine.Utils;
using Sandbox.ModAPI;
using Sandbox.Game.World;
using ScriptManager.Ui;
using ScriptManager.Network;
using Common = ScriptManager.ClientMod.Common;
using VRage.FileSystem;

namespace ScriptManager
{
    public class ScriptManagerPlugin : TorchPluginBase, IWpfPlugin
    {
        //Use this to write messages to the Torch log.
        private static readonly Logger Log = LogManager.GetLogger("ScriptManager");
        private TorchSessionManager _sessionManager;

        private Persistent<ScriptManagerConfig> _config;

        private ScriptManagerUserControl _control;
        //private long ModMessageHandlerId = 36235;
        //private long PluginMessageHandlerId = 59300040;

        public bool IsServerRunning {
            get
            {
                return _sessionManager.CurrentSession.State != TorchSessionState.Unloaded;
            }
        }

#if DEBUG
        public const long MOD_ID = 1478287109; // testing version
#else
        public const long MOD_ID = 1470445959; // stable version
#endif

        public ScriptManagerConfig Config => _config?.Data;

        public static string ScriptsPath { get; private set; }

        public ScriptEntry[] Whitelist
        {
            get
            {
                return _config?.Data.Whitelist.ToArray() ?? new ScriptEntry[0];
            }
        }

        public static ScriptManagerPlugin Instance { get; private set; }
        
        public UserControl GetControl() => _control ?? (_control = new ScriptManagerUserControl() { DataContext = Config, Plugin = this });

        /// <inheritdoc />
        public override void Init(ITorchBase torch)
        {
            base.Init(torch);

            ScriptsPath = Path.Combine(StoragePath, "Scripts");

            _config = Persistent<ScriptManagerConfig>.Load(Path.Combine(StoragePath, "ScriptManager.cfg"));

            ScriptEntry.loadingComplete = true;
            Log.Info("Config has been loaded.");

            Instance = this;

            _sessionManager = Torch.Managers.GetManager<TorchSessionManager>();
            if (_sessionManager != null)
                _sessionManager.SessionStateChanged += OnSessionStateChanged;
            
            var patchMgr = torch.Managers.GetManager<PatchManager>();
            var patchContext = patchMgr.AcquireContext();
            PatchSession(patchContext);
            PatchPB(patchContext);     //apply hooks
            patchMgr.Commit();

            //Your init code here, the game is not initialized at this point.

            Task.Run(delegate
            {
                foreach (var script in _config.Data.Whitelist)
                {
                    string code = "";
                    code = script.Code;
                    script.MD5Hash = Util.GetMD5Hash(code);
                }
            });

            MessageHandler.Init();
        }

        static public void PatchSession(PatchContext context)
        {
            var sessionGetWorld = typeof(MySession).GetMethod(nameof(MySession.GetWorld));
            if (sessionGetWorld == null)
                throw new InvalidOperationException("Couldn't find method MySession.GetWorld!");
            context.GetPattern(sessionGetWorld).Suffixes.Add(typeof(ScriptManagerPlugin).GetMethod(nameof(SuffixGetWorld),
                BindingFlags.Static | BindingFlags.NonPublic));

            
            /* This doesn't work for some reason...
             * 
             * var sessionSave = typeof(MySession).GetMethod(nameof(MySession.Save), 
                new Type[] { typeof(MySessionSnapshot).MakeByRefType(), typeof(string) });
            if (sessionSave == null)
                throw new InvalidOperationException("Couldn't patch method MySession.Save");
            var pattern = context.GetPattern(sessionSave);
            pattern.PrintMsil = true;
            pattern.Prefixes.Add(typeof(ScriptManagerPlugin).GetMethod(nameof(PrefixWorldSave),
                BindingFlags.Static | BindingFlags.NonPublic));*/
        }

        static private bool PrefixWorldSave()
        {
            Instance.OnWorldSave();
            return true;
        }

        static private void SuffixGetWorld(ref MyObjectBuilder_World __result)
        {
            //copy this list so mods added here don't propagate up to the real session
            __result.Checkpoint.Mods = __result.Checkpoint.Mods.ToList();

            if( Instance.Config.Enabled )
                __result.Checkpoint.Mods.Add(new MyObjectBuilder_Checkpoint.ModItem(MOD_ID));

        }

        static public void PatchPB(PatchContext context)
        {
            //var pbCreateInstance = typeof(MyProgrammableBlock).GetMethod("CreateInstance", BindingFlags.Instance | BindingFlags.NonPublic);
            var pbCompile = typeof(MyProgrammableBlock).GetMethod("Compile", BindingFlags.Instance | BindingFlags.NonPublic);

            if (pbCompile == null)
                throw new InvalidOperationException("Couldn't find Compile");
            var checkWhiteListCompile = typeof(ScriptManagerPlugin).GetMethod("CheckWhitelistCompile");
            context.GetPattern(pbCompile).Prefixes.Add(checkWhiteListCompile);

        }

        static public bool CheckWhitelistCompile(string program, string storage, bool instantiate, object __instance = null)
        {
            if (!Instance.Config.Enabled || program == null || program == "")
                return true;

            // exclude npc factions
            var pb = (__instance as MyProgrammableBlock);
            if(CanBypassWhitelist(pb))
                return true;


            //program = program.Replace(" \r", "");
            var whitelist = Instance.Config.Whitelist;
            //var runningScripts = Instance.Config.RunningScripts;
            var scriptHash = Util.GetMD5Hash(program);
            var comparer = StringComparer.OrdinalIgnoreCase;

            if (Instance.Config.ResetScriptEnabled && comparer.Compare(scriptHash, ResetPBScript.MD5) == 0)
            {
                Log.Info($"Script '{ResetPBScript.Name}' found on whitelist! Compiling...");
                return true;
            }

            foreach (var wScript in whitelist)
            {
                if (wScript.Enabled && comparer.Compare(scriptHash, wScript.MD5Hash) == 0)
                {
                    Instance.Config.AddRunningScript(pb.EntityId, wScript);
                    Log.Info($"Script '{wScript.Name}' found on whitelist! Compiling...");
                    return true;
                }
            }
            //_instance?.SetDetailedInfo("Script is not whitelisted. Compilation rejected!");

            //MyMultiplayer.RaiseEvent<MyProgrammableBlock>(__instance, (MyProgrammableBlock x) => x.WriteProgramResponse, msg, default(EndpointId));

            var script = Instance.Config.RunningScripts.GetValueOrDefault(pb.EntityId);
                
            if (script != null && whitelist.Contains(script) && script.Enabled)
            {
                var verifyHash = Util.GetMD5Hash(script.Code);

                // check that script is not already loaded (and compilation failed for other reasons)
                if (comparer.Compare(scriptHash, script.MD5Hash) != 0
                    // and that script code can be successfully hashed
                    && script.Code != null && comparer.Compare(verifyHash, script.MD5Hash) == 0)
                {
                    Log.Info($"PB '{pb.EntityId}' seems to be outdated, updating code (script = {script.Name})...");
                    (pb as IMyProgrammableBlock).ProgramData = script.Code;
                    return false;
                }
            }
            else if( script != null )
            {
                script?.ProgrammableBlocks.Remove(pb.EntityId);
                Instance.Config.RemoveRunningScript(pb.EntityId);
            }

            var msg = "Script is not whitelisted. Compilation rejected!";
            Log.Info(msg);
            Log.Info("Script hash was: <" + scriptHash + ">");


            var setDetailedInfo = typeof(MyProgrammableBlock).GetMethod("SetDetailedInfo", BindingFlags.NonPublic | BindingFlags.Instance);
            if (setDetailedInfo == null)
                throw new InvalidOperationException("method SetDetailedInfo could not be retrieved!");
            Task.Delay(500).ContinueWith(_ =>
            {
                setDetailedInfo.Invoke(__instance, new object[] { msg });
            });

            return false;
        }

        static private bool CanBypassWhitelist(IMyProgrammableBlock pb)
        {
            var shareMode = ((MyCubeBlock)pb).IDModule.ShareMode;
            if ( shareMode == MyOwnershipShareModeEnum.All )
                return false;

            if (!MySession.Static.Players.IdentityIsNpc(pb.OwnerId))
                return false;

            var factionTag = pb.GetOwnerFactionTag();
            var faction = MyAPIGateway.Session.Factions.TryGetFactionByTag(factionTag);
            if (faction == null)
                return true;

            if( shareMode == MyOwnershipShareModeEnum.None || (faction.IsEveryoneNpc() && !faction.AcceptHumans))
                return true;

            return false;
        }

        /// <inheritdoc />
        public override void Update()
        {
            //Put code that should run every tick here.
        }

        /// <inheritdoc />
        public override void Dispose()
        {
            Save();
            //Unload your plugin here.
        }

        public void Save()
        {
            _config.Save();
        }

        private void OnSessionStateChanged(ITorchSession session, TorchSessionState newState)
        {
            switch (newState)
            {
                case TorchSessionState.Loading:
                    Config.LoadRunningScriptsFromWorld();
                    Log.Info("Updating all scripts...");
                    List<Task> taskList = new List<Task>();
                    foreach (var script in Config.Whitelist)
                    {
                        if (script.KeepUpdated)
                            taskList.Add(script.UpdateFromWorkshopAsync());
                    }
                    //Task.WaitAll(taskList.ToArray());
                    break;
                //Executed before the world loads.
                case TorchSessionState.Loaded:
                    //Executed after the world has loaded.
                    MessageHandler.SetupMessaging();
                    //Task.WaitAll(taskList.ToArray());
                    break;
                case TorchSessionState.Unloading:
                    Config.SaveRunningScriptsToWorld();
                    //Executed before the world unloads.
                    break;
                case TorchSessionState.Unloaded:
                    MessageHandler.TearDown();
                    //Executed after the world unloads.
                    break;
            }
        }

        private void OnWorldSave()
        {
            Config.SaveRunningScriptsToWorld();
        }

        private void OnModReady(object data)
        {
            SendWhitelistToMessageHandler();
        }

        private void SendWhitelistToMessageHandler()
        {
            var scriptTitles = new Dictionary<long, string>();
            var scriptBodies = new Dictionary<long, string>();
            foreach(var script in Whitelist)
            {
                if (script.Enabled)
                {
                    scriptTitles.Add(script.Id, script.Name);
                    scriptBodies.Add(script.Id, script.Code);
                }
            }
            var payload = new object[] { "ADD", scriptTitles, scriptBodies };
                
            //MyAPIGateway.Utilities.SendModMessage(ModMessageHandlerId, payload);
        }
    }
}
