using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Torch;
using Torch.API;
using Torch.API.Managers;
using Torch.Session;
using Torch.Views;
using System.Xml.Serialization;
using Sandbox.ModAPI;
using VRage.Game.ModAPI;
using NLog;
using System.ComponentModel;

namespace ScriptManager.Ui
{
    public class ScriptManagerConfig : ViewModel
    {
        private static Logger Log = LogManager.GetLogger("ScriptManager");
        private const string runningPBsVariableKey = "ScriptManager.RunningPbs";
        private const string runningScriptsVariableKey = "ScriptManager.RunningScripts";
        private const string runningScriptsFileName = "ScriptManager_ActiveScripts.xml";

        [XmlIgnore]
        public PropertyChangedEventHandler ScriptEntryChanged;

        [XmlIgnore]
        public Dictionary<long, ScriptEntry> RunningScripts = new Dictionary<long, ScriptEntry>();

        public void LoadRunningScriptsFromWorld()
        {
            Log.Info("Loading running scripts from world...");
            if (MyAPIGateway.Utilities.FileExistsInWorldStorage(runningScriptsFileName, typeof(ScriptManagerConfig)))
            {
                //Dictionary<long, long> runningScripts;

                Dictionary<long, long> runningScripts = null;
                try
                {
                    using (var reader = MyAPIGateway.Utilities.ReadBinaryFileInWorldStorage(runningScriptsFileName, typeof(ScriptManagerConfig)))
                        runningScripts = MyAPIGateway.Utilities.SerializeFromBinary<Dictionary<long, long>>(reader.ReadBytes((int)reader.BaseStream.Length));

                    //runningScripts = MyAPIGateway.Utilities.SerializeFromXML<Dictionary<long, long>>(serialized);
                }
                catch (Exception e)
                {
                    Log.Warn($"Parsing running scripts failed: {e.Message}");
                    return;
                }

                foreach(var kvp in runningScripts)
                {
                    var script = Whitelist.First(item => item.Id == kvp.Value);
                    AddRunningScript(kvp.Key, script);
                }
            }

        }

        public void SaveRunningScriptsToWorld()
        {
            var running = new Dictionary<long, long>();
            Log.Info("Saving running scripts to world...");
            if (RunningScripts != null)
            {
                int i = 0;
                foreach (var kvp in RunningScripts)
                {
                    running[kvp.Key] = kvp.Value.Id;
                    i++;
                }
            }
            byte[] serialized = MyAPIGateway.Utilities.SerializeToBinary(running);
            using (var writer = MyAPIGateway.Utilities.WriteBinaryFileInWorldStorage(runningScriptsFileName, typeof(ScriptManagerConfig)))
                writer.Write(serialized);
        }

        private bool _enabled = true;
        public bool Enabled
        {
            get => _enabled;
            set
            {
                SetValue(ref _enabled, value);
            }
        }

        private bool _resetScriptEnabled = true;
        public bool ResetScriptEnabled
        {
            get => _resetScriptEnabled;
            set
            {
                SetValue(ref _resetScriptEnabled, value);
            }
        }

        private ObservableCollection<ScriptEntry> _whitelist = new ObservableCollection<ScriptEntry>();
        //[Display(EditorType = typeof(EmbeddedCollectionEditor))]
        public ObservableCollection<ScriptEntry> Whitelist
        {
            get { return _whitelist; }
            set { SetValue(ref _whitelist, value); }
        }

        public ScriptManagerConfig() : base()
        {
            Whitelist.CollectionChanged += (object sender, NotifyCollectionChangedEventArgs e) => {
                if (e.Action == NotifyCollectionChangedAction.Add || e.Action == NotifyCollectionChangedAction.Replace)
                    foreach (var item in e.NewItems)
                        (item as ScriptEntry).PropertyChanged += (script, propertyChangedEv) =>
                        {
                            OnScriptEntryChanged(script, propertyChangedEv);
                        };
                OnPropertyChanged(nameof(Whitelist));
            };

        }

        public void AddRunningScript(long pbId, ScriptEntry script)
        {
            if (RunningScripts.ContainsKey(pbId))
                RunningScripts[pbId].ProgrammableBlocks.Remove(pbId);
            if (!script.ProgrammableBlocks.Contains(pbId))
                script.ProgrammableBlocks.Add(pbId);
            RunningScripts[pbId] = script;
            //SaveRunningScriptsToWorld();
        }

        private void OnScriptEntryChanged(object sender, PropertyChangedEventArgs e)
        {
            ScriptEntryChanged?.Invoke(sender, e);
        }
    }

    public class ActiveScriptsCollectionChangedAction
    {
        public long PbId;
        public long ScriptId;
        public enum ScriptsCollectionChangedAction { ADD, REMOVE };
        public ScriptsCollectionChangedAction Action;
    }
}
