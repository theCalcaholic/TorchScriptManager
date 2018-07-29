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
using VRage.Game.ModAPI.Ingame;
using Sandbox.Game.EntityComponents;
using SpaceEngineers.Game.ModAPI.Ingame;
using Sandbox.Game.Entities.Blocks;
using Sandbox.Engine.Multiplayer;
using System.Security.Cryptography;
using Sandbox.Engine.Utils;

namespace ScriptManager
{
    public class ScriptManagerPlugin : TorchPluginBase, IWpfPlugin
    {
        //Use this to write messages to the Torch log.
        private static readonly Logger Log = LogManager.GetLogger("ScriptManager");
        private TorchSessionManager _sessionManager;

        private Persistent<ScriptManagerConfig> _config;

        private UserControl _control;

        public ScriptManagerConfig Config => _config?.Data;

        private static MD5 md5Hash;

        public ScriptEntry[] Whitelist
        {
            get
            {
                return _config?.Data.Whitelist.ToArray() ?? new ScriptEntry[0];
            }
        }

        public static ScriptManagerPlugin Instance { get; private set; }

        /// <inheritdoc />
        //public UserControl GetControl() {
        //    return _control ?? (_control = new ScriptManagerControl(this));
        //}
        public UserControl GetControl() => _control ?? (_control = new PropertyGrid() { DataContext = Config, IsEnabled = true });

        /// <inheritdoc />
        public override void Init(ITorchBase torch)
        {
            base.Init(torch);
            _config = Persistent<ScriptManagerConfig>.Load(Path.Combine(StoragePath, "ScriptManager.cfg"));
            md5Hash = MD5.Create();
            _sessionManager = Torch.Managers.GetManager<TorchSessionManager>();
            //if (_sessionManager != null)
            //    _sessionManager.SessionStateChanged += SessionChanged;
            var patchMgr = torch.Managers.GetManager<PatchManager>();
            var patchContext = patchMgr.AcquireContext();
            ScriptManagerPlugin.Apply(patchContext);     //apply hooks
            patchMgr.Commit();
            //Your init code here, the game is not initialized at this point.
            Instance = this;
        }

        static public void Apply(PatchContext context)
        {
            //var pbCreateInstance = typeof(MyProgrammableBlock).GetMethod("CreateInstance", BindingFlags.Instance | BindingFlags.NonPublic);
            var pbCompile = typeof(MyProgrammableBlock).GetMethod("Compile", BindingFlags.Instance | BindingFlags.NonPublic);

            if (pbCompile == null)
                throw new InvalidOperationException("Couldn't find Compile");
            var checkWhiteListCompile = typeof(ScriptManagerPlugin).GetMethod("CheckWhitelistCompile");
            if (checkWhiteListCompile == null)
                throw new InvalidOperationException("METHOD NOT FOUND!!!");
            context.GetPattern(pbCompile).Prefixes.Add(checkWhiteListCompile);


            var pbRecompile = typeof(MyProgrammableBlock).GetMethod("Recompile", BindingFlags.Instance | BindingFlags.NonPublic, Type.DefaultBinder, new Type[] { typeof(bool) }, null);
            if (pbRecompile == null)
                throw new InvalidOperationException("Couldn't find Recompile!");
            var block = typeof(ScriptManagerPlugin).GetMethod("Block");
            context.GetPattern(pbRecompile).Prefixes.Add(block);

            /*var pbUpdateProgram = typeof(MyProgrammableBlock).GetMethod("UpdateProgram"); //, BindingFlags.Instance | BindingFlags.NonPublic, Type.DefaultBinder, new Type[] { typeof(string) }, null);
            if (pbUpdateProgram == null)
                throw new InvalidOperationException("Couldn't find UpdateProgram");
            var checkWhiteListUpdateProgram = typeof(ScriptManagerPlugin).GetMethod("CheckWhitelistUpdateProgram");
            context.GetPattern(pbUpdateProgram).Prefixes.Add(checkWhiteListUpdateProgram);*/

            var pbCreateInstance = typeof(MyProgrammableBlock).GetMethod("CreateInstance", BindingFlags.Instance | BindingFlags.NonPublic);
            if (pbCreateInstance == null)
                throw new Exception("Couldn't find CreateInstance!");
            var logCreateInstanceExecution = typeof(ScriptManagerPlugin).GetMethod("LogCreateInstanceExecution");
            context.GetPattern(pbCreateInstance).Suffixes.Add(logCreateInstanceExecution);

            var logCompileExecution = typeof(ScriptManagerPlugin).GetMethod("LogCompileExecution");
            context.GetPattern(pbCompile).Suffixes.Add(logCompileExecution);
        }

        static public bool Block(bool instantiate, object __instance = null)
        {
            Log.Info("Blocking Recompile...");
            return false;
        }

        static public bool CheckWhitelistCompile(string program, string storage, ref bool instantiate, object __instance = null)
        {
            var md5 = MD5.Create();
            var scriptHash = ScriptManagerPlugin.GetMD5Hash(program);
            var comparer = StringComparer.OrdinalIgnoreCase;
            foreach(var script in ScriptManagerPlugin.Instance.Whitelist)
            {
                if (script.Enabled && comparer.Compare(scriptHash, script.MD5Hash) == 0)
                {
                    Log.Info("Script found on whitelist! Compiling...");
                    return true;
                }
            }
            instantiate = false;
            //_instance?.SetDetailedInfo("Script is not whitelisted. Compilation rejected!");
            var msg = "Script is not whitelisted. Compilation rejected!";
            Log.Info(msg);
            Log.Info("Script hash was: <" + scriptHash + ">");

            var setDetailedInfo = typeof(MyProgrammableBlock).GetMethod("SetDetailedInfo", BindingFlags.NonPublic | BindingFlags.Instance);
            if (setDetailedInfo == null)
                throw new InvalidOperationException("method SetDetailedInfo could not be retrieved!");
            setDetailedInfo.Invoke(__instance, new object[] { msg });

            //MyMultiplayer.RaiseEvent<MyProgrammableBlock>(__instance, (MyProgrammableBlock x) => x.WriteProgramResponse, msg, default(EndpointId));
            return false;
        }

        static public bool CheckWhitelistUpdateProgram(string program, MyProgrammableBlock __instance)
        {
            var md5 = MD5.Create();
            var scriptHash = ScriptManagerPlugin.GetMD5Hash(program);
            var comparer = StringComparer.OrdinalIgnoreCase;
            foreach (var script in ScriptManagerPlugin.Instance.Whitelist)
            {
                if (script.Enabled && comparer.Compare(scriptHash, script.MD5Hash) == 0)
                {
                    Log.Info("Script found on whitelist! Compiling...");
                    return true;
                }
            }
            //_instance?.SetDetailedInfo("Script is not whitelisted. Compilation rejected!");
            var msg = "Script is not whitelisted. Compilation rejected!";
            Log.Info(msg);
            Log.Info("Script hash was: <" + scriptHash + ">");

            var setDetailedInfo = typeof(MyProgrammableBlock).GetMethod("SetDetailedInfo", BindingFlags.NonPublic | BindingFlags.Instance);
            if (setDetailedInfo == null)
                throw new InvalidOperationException("method SetDetailedInfo could not be retrieved!");
            setDetailedInfo.Invoke(__instance, new object[] { msg });

            //MyMultiplayer.RaiseEvent<MyProgrammableBlock>(__instance, (MyProgrammableBlock x) => x.WriteProgramResponse, msg, default(EndpointId));
            return false;
        }

        static public void LogCompileExecution(string program, string storage, bool instantiate = false, object __instance = null)
        {
            Log.Info("Compile was executed.");
        }

        static public void LogCreateInstanceExecution(Assembly assembly, IEnumerable<string> messages, string storage, object __instance = null)
        {
            Log.Info("Compile was executed.");
        }


        static public string GetMD5Hash(string input)
        {
            byte[] data = ScriptManagerPlugin.md5Hash.ComputeHash(Encoding.UTF8.GetBytes(input));
            var sBuilder = new StringBuilder();

            for( int i = 0; i < data.Length; i++ )
            {
                sBuilder.Append(data[i].ToString("x2"));
            }
            return sBuilder.ToString();
        }

        /// <inheritdoc />
        public override void Update()
        {
            //Put code that should run every tick here.
        }

        /// <inheritdoc />
        public override void Dispose()
        {
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
                    //Executed before the world loads.
                    break;
                case TorchSessionState.Loaded:
                    //Executed after the world has finished loading.
                    break;
                case TorchSessionState.Unloading:
                    //Executed before the world unloads.
                    break;
                case TorchSessionState.Unloaded:
                    //Executed after the world unloads.
                    break;
            }
        }
    }
}
