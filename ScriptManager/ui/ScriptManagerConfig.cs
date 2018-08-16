using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Torch;
using Torch.Views;
using System.Xml.Serialization;
using NLog;
using System.ComponentModel;

namespace ScriptManager.Ui
{
    public class ScriptManagerConfig : ViewModel
    {
        private bool _enabled = true;

        [XmlIgnore]
        public PropertyChangedEventHandler ScriptEntryChanged;

        [XmlIgnore]
        public Dictionary<long, ScriptEntry> RunningScripts = new Dictionary<long, ScriptEntry>();

        public bool Enabled
        {
            get => _enabled;
            set
            {
                _enabled = value;
                OnPropertyChanged();
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
                if( e.Action == NotifyCollectionChangedAction.Add || e.Action == NotifyCollectionChangedAction.Replace)
                    foreach(var item in e.NewItems)
                        (item as ScriptEntry).PropertyChanged += (script, propertyChangedEv) =>
                        {
                            OnScriptEntryChanged(script, propertyChangedEv);
                        };
                OnPropertyChanged(nameof(Whitelist));
            };

        }

        private void OnScriptEntryChanged(object sender, PropertyChangedEventArgs e)
        {
            ScriptEntryChanged?.Invoke(sender, e);
        }
    }
}
