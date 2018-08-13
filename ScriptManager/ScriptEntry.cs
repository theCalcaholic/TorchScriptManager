using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Torch;
using Torch.API;
using TViews = Torch.Views;
using Torch.Server;
using Torch.Commands;
using System.ComponentModel;

namespace ScriptManager
{
    public class ScriptEntry : ViewModel
    {
        private bool _enabled;

        private long _id;

        public ScriptEntry()
        {
            Id = nextId++;
        }
        public bool Enabled
        {
            get => _enabled;
            set
            {
                _enabled = value;
                OnPropertyChanged();
            }
        }
        public long Id
        {
            get => _id;
            set
            {
                if( _id > nextId )
                {
                    Log.Warn("Invalid script Id! Id will be changed.");
                    _id = nextId++;
                }
                else
                {
                    _id = value;
                    nextId = _id + 1;
                }

                OnPropertyChanged();
            }
        }

        private string _name;
        [TViews.Display(Description = "Script Name")]
        public string Name
        {
            get => _name;
            set
            {
                _name = value;
                OnPropertyChanged();
            }
        }

        private string _md5Hash;
        [TViews.Display(Name = "MD5 Hash", Description = "MD5 Hash of the script's code.")]
        public string MD5Hash
        {
            get => _md5Hash;
            set
            {
                _md5Hash = value;
                OnPropertyChanged();
            }
        }

        private string _code;
        public string Code
        {
            get => _code;
            set
            {
                _code = value;
                OnPropertyChanged();
            }
        }

        [XmlIgnore]
        public int InstallCount
        {
            get => ProgrammableBlocks.Count;
            set { OnPropertyChanged(); }
        }

        private ObservableCollection<long> _programmableBlocks = new ObservableCollection<long>();
        [XmlIgnore]
        public ObservableCollection<long> ProgrammableBlocks
        {
            get => _programmableBlocks;
            set {
                SetValue(ref _programmableBlocks, value);
                OnPropertyChanged("InstallCount");
            }
        }

        public ScriptEntry()
        {
            _programmableBlocks.CollectionChanged += (object sender, NotifyCollectionChangedEventArgs e) => {
                OnPropertyChanged("InstallCount");
            };
        }

        
    }
}
