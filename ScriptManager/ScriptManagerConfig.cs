using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Torch;
using Torch.Views;

namespace ScriptManager
{
    public class ScriptManagerConfig : ViewModel
    {
        private ObservableCollection<ScriptEntry> _whitelist = new ObservableCollection<ScriptEntry>();
        [Display(EditorType = typeof(EmbeddedCollectionEditor))]
        public ObservableCollection<ScriptEntry> Whitelist
        {
            get { return _whitelist; }
            set { SetValue(ref _whitelist, value); }
        }
    }
}
