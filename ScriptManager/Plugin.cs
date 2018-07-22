using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NLog;
using Torch;
using Torch.API;
using Torch.API.Session;

namespace ScriptManager
{
    public class Plugin : TorchPluginBase
    {
        //Use this to write messages to the Torch log.
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        /// <inheritdoc />
        public override void Init(ITorchBase torch)
        {
            base.Init(torch);
            torch.CurrentSession.StateChanged += OnSessionStateChanged;
            //Your init code here, the game is not initialized at this point.
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
