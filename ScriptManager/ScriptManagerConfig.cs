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

namespace ScriptManager
{
    public class ScriptManagerConfig : ViewModel
    {
        private bool _enabled = true;

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
            Whitelist.CollectionChanged += (object sender, NotifyCollectionChangedEventArgs e) => { OnPropertyChanged(); };
        }
    }
}
