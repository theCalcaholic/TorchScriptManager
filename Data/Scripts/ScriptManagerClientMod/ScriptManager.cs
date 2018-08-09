using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox.Game;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces.Terminal;
using VRage.Game.Components;
using VRage.ObjectBuilders.Definitions;
using Sandbox.Common.ObjectBuilders.Definitions;
using VRage.ModAPI;
using VRage.Game;
using VRage.Utils;

namespace ScriptManagerClientMod
{
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_ProgrammableBlockDefinition), false, new string[] { "SmallProgrammableBlock", "LargeProgrammableBlock" })]
    public class ScriptManager : MyGameLogicComponent
    {
        IMyTerminalControlCombobox ScriptsDropdown;

        public override void Init(MyComponentDefinitionBase definition)
        {
            ScriptsDropdown =
                MyAPIGateway.TerminalControls.CreateControl<
                    IMyTerminalControlCombobox,
                    IMyProgrammableBlock>("Script Whitelist");
            ScriptsDropdown.Setter += UpdateScriptFromWhitelist;
        }
        public override void OnAddedToContainer()
        {
            var pb = Container.Entity as IMyProgrammableBlock;
            List<IMyTerminalControl> controls = new List<IMyTerminalControl>();
            MyAPIGateway.TerminalControls.GetControls<IMyProgrammableBlock>(out controls);
            MyLog.Default.WriteLine("PB controls:");
            foreach(var control in controls)
            {
                MyLog.Default.WriteLine(control.Id);
            }

        }

        private void UpdateScriptFromWhitelist(IMyTerminalBlock b, long l)
        {

        }
    }
}
