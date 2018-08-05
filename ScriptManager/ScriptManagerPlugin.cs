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

namespace ScriptManager
{
    public class ScriptManagerPlugin : TorchPluginBase, IWpfPlugin
    {
        //Use this to write messages to the Torch log.
        private static readonly Logger Log = LogManager.GetLogger("ScriptManager");
        private TorchSessionManager _sessionManager;

        private Persistent<ScriptManagerConfig> _config;

        private ScriptManagerUserControl _control;

        public ScriptManagerConfig Config => _config?.Data;

        private static MD5 md5Hash;

        private static string BlockedScript = @"
public Program()
{
    throw new Exception(""Script was blocked by Whitelist!"");
}



public void Save()
{
    throw new Exception(""Script was blocked by Whitelist!"");
}



public void Main(string argument, UpdateType updateSource)
{
    throw new Exception(""Script was blocked by Whitelist!"");
}
";

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
        public UserControl GetControl() => _control ?? (_control = new ScriptManagerUserControl() { DataContext = Config, Plugin = this });

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
            ScriptManagerPlugin.PatchPB(patchContext);     //apply hooks
            patchMgr.Commit();
            //Your init code here, the game is not initialized at this point.
            Instance = this;
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
            if (Instance.Config.Enabled && program == null)
                return true;
            program = program.Replace("\r", "");
            var scriptHash = GetMD5Hash(program);
            var comparer = StringComparer.OrdinalIgnoreCase;
            foreach(var script in Instance.Whitelist)
            {
                if (script.Enabled && comparer.Compare(scriptHash, script.MD5Hash) == 0)
                {
                    Log.Info("Script found on whitelist! Compiling...");
                    return true;
                }
            }
            //_instance?.SetDetailedInfo("Script is not whitelisted. Compilation rejected!");

            //MyMultiplayer.RaiseEvent<MyProgrammableBlock>(__instance, (MyProgrammableBlock x) => x.WriteProgramResponse, msg, default(EndpointId));

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
