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
using VRage.Scripting;
using VRage.Utils;
using VRage.Network;
using VRageMath;
using VRage.Collections;
using Sandbox.ModAPI.Ingame;
using Sandbox.Game.Entities;
using Sandbox.Common.ObjectBuilders;
using VRage.Game.ModAPI.Ingame;
using Sandbox.Game.EntityComponents;
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
        private bool IsClientModReady = false;
        //private long ModMessageHandlerId = 36235;
        //private long PluginMessageHandlerId = 59300040;

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
            _config.Data.SaveLoadMode = false;

            _sessionManager = Torch.Managers.GetManager<TorchSessionManager>();
            if (_sessionManager != null)
                _sessionManager.SessionStateChanged += OnSessionStateChanged;
            var patchMgr = torch.Managers.GetManager<PatchManager>();
            var patchContext = patchMgr.AcquireContext();
            PatchModlist(patchContext);
            PatchPB(patchContext);     //apply hooks
            patchMgr.Commit();
            //Your init code here, the game is not initialized at this point.
            Instance = this;

            MessageHandler.Init();

            foreach (var script in _config.Data.Whitelist)
            {
                string code = "";
                code = script.Code;
                Log.Info($"Got code of length {code.Length}");
                script.MD5Hash = Util.GetMD5Hash(code);
            }
        }

        static public void PatchModlist(PatchContext context)
        {
            var sessionGetWorld = typeof(MySession).GetMethod(nameof(MySession.GetWorld));
            if (sessionGetWorld == null)
                throw new InvalidOperationException("Couldn't find method MySession.GetWorld!");

            context.GetPattern(sessionGetWorld).Suffixes.Add(typeof(ScriptManagerPlugin).GetMethod(nameof(SuffixGetWorld),
                BindingFlags.Static | BindingFlags.NonPublic));
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
            var factionTag = pb.GetOwnerFactionTag();
            var faction = MyAPIGateway.Session.Factions.TryGetFactionByTag(factionTag);
            if (faction != null && faction.IsEveryoneNpc() && !faction.AcceptHumans)
                return true;


            //program = program.Replace(" \r", "");
            var whitelist = Instance.Config.Whitelist;
            var runningScripts = Instance.Config.RunningScripts;
            var scriptHash = Util.GetMD5Hash(program);
            var comparer = StringComparer.OrdinalIgnoreCase;
            foreach (var script in whitelist)
            {
                if (script.Enabled && comparer.Compare(scriptHash, script.MD5Hash) == 0)
                {
                    if(!script.ProgrammableBlocks.Contains(pb.EntityId))
                        script.ProgrammableBlocks.Add(pb.EntityId);
                    runningScripts[pb.EntityId] = script;
                    Log.Info("Script found on whitelist! Compiling...");
                    return true;
                }
                /*else
                {
                    Log.Info("Script is different to " + script.Name);
                    var comparison = "";
                    for (int i = 0; i < Math.Max(program.Length, script.Code.Length); i++)
                    {
                        if( i >= program.Length )
                        {
                            comparison += "<|" + script.Code[i] + ">";
                        }
                        else if( i >= script.Code.Length )
                        {
                            comparison += "<" + program[i] + "|>";
                        }
                        else if( program[i] == script.Code[i] )
                        {
                            comparison += program[i];
                        }
                        else
                        {
                            comparison += "<" + program[i] + "|" + script.Code[i] + ">";
                        }
                    }
                    comparison = comparison.Replace("\r", "\\r");
                    comparison = comparison.Replace("\n", "\\n");
                    comparison = comparison.Replace("\t", "\\t");
                    Log.Info(comparison);
                }*/
            }
            //_instance?.SetDetailedInfo("Script is not whitelisted. Compilation rejected!");

            //MyMultiplayer.RaiseEvent<MyProgrammableBlock>(__instance, (MyProgrammableBlock x) => x.WriteProgramResponse, msg, default(EndpointId));

            if (runningScripts.ContainsKey(pb.EntityId))
            {

                if (whitelist.Contains(runningScripts[pb.EntityId]))
                {
                    (pb as Sandbox.ModAPI.IMyProgrammableBlock).ProgramData = runningScripts[pb.EntityId].Code;
                    Log.Info($"PB '{pb.EntityId}' seems to be outdated, updating code (script = {runningScripts[pb.EntityId].Name})...");
                    return false;
                }
                else
                {
                    runningScripts[pb.EntityId]?.ProgrammableBlocks.Remove(pb.EntityId);
                    runningScripts.Remove(pb.EntityId);
                    
                }
            }

            var msg = "Script is not whitelisted. Compilation rejected!";
            Log.Info(msg);
            Log.Info("Script hash was: <" + scriptHash + ">");


            var setDetailedInfo = typeof(MyProgrammableBlock).GetMethod("SetDetailedInfo", BindingFlags.NonPublic | BindingFlags.Instance);
            if (setDetailedInfo == null)
                throw new InvalidOperationException("method SetDetailedInfo could not be retrieved!");
            Task.Delay(300).ContinueWith(_ =>
            {
                setDetailedInfo.Invoke(__instance, new object[] { msg });
            });

            return false;
        }

        /*static public void SetPBDetailedInfo(string info, object instance)
        {
            var setDetailedInfo = typeof(MyProgrammableBlock).GetMethod("SetDetailedInfo", BindingFlags.NonPublic | BindingFlags.Instance);
            if (setDetailedInfo == null)
                throw new InvalidOperationException("method SetDetailedInfo could not be retrieved!");
            setDetailedInfo.Invoke(instance, new object[] { info });
        }*/

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
            Config.SaveLoadMode = true;
            _config.Save();
            Config.SaveLoadMode = false;
        }

        private void OnSessionStateChanged(ITorchSession session, TorchSessionState newState)
        {
            switch (newState)
            {
                case TorchSessionState.Loading:
                    Log.Info("Asynchronously updating all scripts...");
                    List<Task> taskList = new List<Task>();
                    foreach (var script in Config.Whitelist)
                    {
                        if (script.Enabled)
                            foreach (var pbId in script.ProgrammableBlocks)
                                if (!Config.RunningScripts.ContainsKey(pbId))
                                    Config.RunningScripts[pbId] = script;
                        if (script.KeepUpdated)
                            taskList.Add(script.UpdateFromWorkshopAsync());
                    }
                    break;
                //Executed before the world loads.
                case TorchSessionState.Loaded:
                    MessageHandler.SetupMessaging();
                    //Task.WaitAll(taskList.ToArray());
                    //Executed after the world has loaded.
                    break;
                case TorchSessionState.Unloading:
                    //Executed before the world unloads.
                    break;
                case TorchSessionState.Unloaded:
                    //Executed after the world unloads.
                    break;
            }
        }

        private void OnModReady(object data)
        {
            IsClientModReady = true;
            SendWhitelistToMod();
        }

        private void SendWhitelistToMod()
        {
            Log.Info("Transmitting Whitelist to mod!");
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
