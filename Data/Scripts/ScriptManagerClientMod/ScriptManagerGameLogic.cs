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


namespace ScriptManagerClientMod
{
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_MyProgrammableBlock), false, new string[] { "SmallProgrammableBlock", "LargeProgrammableBlock" })]
    public class ScriptManagerGameLogic : MyGameLogicComponent
    {
        IMyTerminalControlCombobox ScriptsDropdown;
        static bool Initialized = false;
        static KeyValuePair<long, string> NOSCRIPT =
            new KeyValuePair<long, string>(-1L, "NONE");

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
                foreach (var script in CommonData.Scripts)
                {
                    items.Add(new MyTerminalControlComboBoxItem()
                    {
                        Key = script.Key,
                        Value = MyStringId.GetOrCompute(script.Value)
                    });
                }
            };
            ScriptsDropdown.Getter = GetActiveScript;
            ScriptsDropdown.Setter = delegate (IMyTerminalBlock b, long l)
            {
                if (b.Storage == null)
                    b.Storage = new MyModStorageComponent();
                b.Storage[CommonData.GUID] = l.ToString();
                //(b as IMyProgrammableBlock).ProgramData = Scripts[l].Code;
                ScriptManagerCore.RequestPBRecompile(b as IMyProgrammableBlock);
            };

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

        public static long GetActiveScript(IMyTerminalBlock pb)
        {
            string activeScript;
            long l = NOSCRIPT.Key;
            if (pb.Storage != null && pb.Storage.ContainsKey(CommonData.GUID))
                Int64.TryParse(pb.Storage[CommonData.GUID], out l);
            if ( CommonData.Scripts.ContainsKey(l) )
                return l;
            return NOSCRIPT.Key;
        }

    }
}
