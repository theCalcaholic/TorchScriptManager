using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox.Game;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces.Terminal;
using Sandbox.Game.EntityComponents;
using VRage.Game.Components;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Engine.Multiplayer;
using VRage.ModAPI;
using VRage.Game;
using VRage.Game.ModAPI;
using VRage.Utils;
using VRage.Network;
using ScriptManager.ClientMod.Common;



namespace ScriptManager.ClientMod
{
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_MyProgrammableBlock), false, new string[] { "SmallProgrammableBlock", "LargeProgrammableBlock" })]
    public class ScriptManagerGameLogic : MyGameLogicComponent
    {
        IMyTerminalControlCombobox ScriptsDropdown;
        static bool Initialized = false;

        public override void OnAddedToContainer()
        {
            if (Initialized)
                return;

            /*if (MyAPIGateway.Session.IsServer)
            {
                MyAPIGateway.Utilities.RegisterMessageHandler(ScriptManagerMessageHandlerId, ReceiveWhitelist);
                MyAPIGateway.Utilities.SendModMessage(ScriptManagerMessageHandlerId, null);
            }*/


            /*ReceiveWhitelist(new Dictionary<string, string> { { "TestScript", @"public Program()
{
Echo(""Hello World!"");
}

public void Main(string argument)
{
Echo(""Main"");
}" } });*/

            AddPBScriptSelector();
            HideEditButton();

            Initialized = true;

        }

        private void AddPBScriptSelector()
        {
            ScriptsDropdown =
                MyAPIGateway.TerminalControls.CreateControl<
                    IMyTerminalControlCombobox,
                    IMyProgrammableBlock>("ScriptWhitelist");
            ScriptsDropdown.Title = MyStringId.GetOrCompute("Active Script");
            MyAPIGateway.TerminalControls.AddControl<IMyProgrammableBlock>(ScriptsDropdown);

            ScriptsDropdown.ComboBoxContent = (List<MyTerminalControlComboBoxItem> items) => {
                foreach (var script in WhitelistData.Scripts)
                {
                    items.Add(new MyTerminalControlComboBoxItem()
                    {
                        Key = script.Key,
                        Value = MyStringId.GetOrCompute(script.Value)
                    });
                }
            };
            ScriptsDropdown.Getter = GetActiveScript;
            ScriptsDropdown.Setter = SetActiveScript;

        }

        private void HideEditButton()
        {
            List<IMyTerminalControl> controls = new List<IMyTerminalControl>();
            MyAPIGateway.TerminalControls.GetControls<IMyProgrammableBlock>(out controls);
            var editButton = controls.Find((IMyTerminalControl control) => control.Id == "Edit") as IMyTerminalControlButton;
            var isVisible = editButton.Visible;
            editButton.Visible = (b) =>
            {
                var subtype = b.SlimBlock.BlockDefinition.Id.SubtypeId;
                if (subtype == MyStringHash.GetOrCompute("SmallProgrammableBlock")
                    || subtype == MyStringHash.GetOrCompute("LargeProgrammableBlock"))
                    return false;
                return isVisible?.Invoke(b) ?? false;
            };
        }


        private static long GetActiveScript(IMyTerminalBlock pb)
        {
            long l = Config.NOSCRIPT.Key;
            if (pb.Storage != null && pb.Storage.ContainsKey(Config.GUID))
            {
                Int64.TryParse(pb.Storage[Config.GUID], out l);
            }
            if (WhitelistData.Scripts.ContainsKey(l))
            {
                return l;
            }
            ModLogger.Warning("No script with id '{0}' found in Whitelist.", l);
            return Config.NOSCRIPT.Key;
        }

        private static void SetActiveScript(IMyTerminalBlock pb, long l)
        {
            if (pb.Storage == null)
                pb.Storage = new MyModStorageComponent();
            pb.Storage[Config.GUID] = l.ToString();
            //(b as IMyProgrammableBlock).ProgramData = Scripts[l].Code;
            if (!MyAPIGateway.Multiplayer.IsServer)
                ScriptManagerCore.RequestPBRecompile(pb as IMyProgrammableBlock, l);
        }

    }
}
