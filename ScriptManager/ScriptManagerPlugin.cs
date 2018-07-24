using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;

using NLog;
using Torch;
using Torch.Managers.PatchManager;
using Torch.API;
using Torch.API.Session;
using Torch.API.Managers;
using Torch.API.Plugins;
using Torch.Session;
using Torch.API.Event;

using Sandbox.ModAPI.Interfaces;
using VRage.Game.Components;
using VRage.Game.ObjectBuilders.Definitions;
using VRage.Game;
using VRageMath;
using VRage.Collections;
using Sandbox.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame;
using Sandbox.Game.EntityComponents;
using SpaceEngineers.Game.ModAPI.Ingame;
using Sandbox.Game.Entities.Blocks;

namespace ScriptManager
{
    public class ScriptManagerPlugin : TorchPluginBase
    {
        //Use this to write messages to the Torch log.
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private TorchSessionManager _sessionManager;

        private static string[] whitelist = { };

        /// <inheritdoc />
        public override void Init(ITorchBase torch)
        {
            base.Init(torch);
            _sessionManager = Torch.Managers.GetManager<TorchSessionManager>();
            //if (_sessionManager != null)
            //    _sessionManager.SessionStateChanged += SessionChanged;
            var patchMgr = torch.Managers.GetManager<PatchManager>();
            var patchContext = patchMgr.AcquireContext();
            ScriptManagerPlugin.Apply(patchContext);     //apply hooks
            patchMgr.Commit();
            //Your init code here, the game is not initialized at this point.
        }

        static public void Apply(PatchContext context)
        {
            var hookedmethod = typeof(MyProgrammableBlock).GetMethod("Compile", BindingFlags.Instance | BindingFlags.NonPublic);
            if (hookedmethod == null)
                throw new InvalidOperationException("Couldn't find Compile");
            var method = typeof(ScriptManagerPlugin).GetMethod("CheckScriptHash", new[] { typeof(string), typeof(string), typeof(bool) });
            context.GetPattern(hookedmethod).Prefixes.Add(method);
        }

        static public void CheckScriptHash(string program, string storage, bool instantiate = false)
        {
            throw new Exception("Script is not whitelisted. Compilation rejected!");
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
