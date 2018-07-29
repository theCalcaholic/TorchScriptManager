using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Torch;
using Torch.API;
using Torch.Views;
using Torch.Server;
using Torch.Commands;

namespace ScriptManager
{
    public class ScriptEntry : ViewModel
    {
        private bool _enabled;
        public bool Enabled
        {
            get => _enabled;
            set
            {
                _enabled = value;
                OnPropertyChanged();
            }
        }

        private string _name;
        [Display(Description = "Script Name")]
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
        [Display(Name = "MD5 Hash", Description = "MD5 Hash of the script's code.")]
        public string MD5Hash
        {
            get => _md5Hash;
            set
            {
                _md5Hash = value;
                OnPropertyChanged();
            }
        }
    }
}
