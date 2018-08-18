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
using NLog;
using System.Xml.Serialization;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using Sandbox.Engine.Networking;
using VRage.FileSystem;
using System.IO;
using Sandbox.ModAPI;

namespace ScriptManager.Ui
{
    public class ScriptEntry : ViewModel
    {
        private static Logger Log = LogManager.GetLogger("ScriptManager");
        private bool _enabled;
        private static long nextId = 0;
        private bool _needsUpdate = true;

        private long _id;

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
            get
            {
                return _code;
            }
            set
            {
                _code = value;
                MD5Hash = Util.GetMD5Hash(_code);
                OnPropertyChanged();
            }
        }

        private ulong _workshopId;
        public ulong WorkshopID
        { get => _workshopId; set
            {
                _workshopId = value;
                OnPropertyChanged();
            }
        }

        private bool _keepUpdated;
        public bool KeepUpdated
        {
            get => _keepUpdated;
            set
            {
                _keepUpdated = value;
                OnPropertyChanged();
            }
        }

        [XmlIgnore]
        public int InstallCount
        {
            get => ProgrammableBlocks.Count;
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
            Id = nextId++;
            _programmableBlocks.CollectionChanged += (object sender, NotifyCollectionChangedEventArgs e) => {
                OnPropertyChanged("InstallCount");
            };
            PropertyChanged += (object sender, PropertyChangedEventArgs e) =>
            {
                if (e.PropertyName == nameof(Code))
                    UpdateRunning();
                else if (e.PropertyName == nameof(WorkshopID))
                    UpdateFromWorkshopAsync();
            };
        }

        static public ScriptEntry CreateFromWorkshopId(ulong workshopId, bool keepUpdated = false)
        {

            if (workshopId == 0)
                throw new Exception("Invalid workshop ID!");

            var fetchScriptInfoTask = WorkshopTools.GetScriptInfoAsync(workshopId);
            fetchScriptInfoTask.Wait();
            var scriptInfo = fetchScriptInfoTask.Result;

            var script = new ScriptEntry()
            {
                Name = scriptInfo.Title,
                MD5Hash = "",
                Code = "",
                KeepUpdated = keepUpdated
            };

            return script;
        }

        /*public async Task<bool> UpdateFromWorkshopAsync()
        {
            return await Task.Run(delegate {
                return UpdateFromWorkshopBlocking();
            });
        }*/

        public async Task<bool> UpdateFromWorkshopAsync()
        {
            if (WorkshopID == 0 || !KeepUpdated || !_needsUpdate)
                return true;

            if (!MyGameService.IsOnline)
                return false;

            Log.Info($"Updating script '{Name}'");

            MyWorkshop.SubscribedItem scriptInfo = await WorkshopTools.GetScriptInfoAsync(WorkshopID);
            if( scriptInfo == null )
            {
                Log.Warn($"An error occured while fetching script info for '{Name}'.");
                return false;
            }

            Log.Info($"Fetched script info for '{Name}'.");

            Name = scriptInfo.Title;

            var code = await WorkshopTools.DownloadScriptAsync(scriptInfo);

            if (code == null)
                return false;

            Code = code;
            return true;
        }

        /*public static bool DownloadScriptBlocking(MyWorkshop.SubscribedItem item)
        {
            if (!MyGameService.IsOnline)
            {
                return false;
            }
            string text = Path.Combine(MyFileSystem., item.PublishedFileId + ".sbs");
            if (!MyWorkshop.IsModUpToDateBlocking(text, item, true, -1L))
            {
                if (!MyWorkshop.DownloadItemBlocking(text, item.UGCHandle))
                {
                    return false;
                }
            }
            else
            {
                MySandboxGame.Log.WriteLineAndConsole($"Up to date mod: Id = {item.PublishedFileId}, title = '{item.Title}'");
            }
            return true;
        }*/

        private void UpdateRunning()
        {
            foreach(var pbId in ProgrammableBlocks )
            {
                if (MyAPIGateway.Entities.GetEntityById(pbId) is IMyProgrammableBlock pb)
                    pb.ProgramData = Code;
            }
        }

    }

    public class ScriptNotFoundException : Exception
    {
        public ScriptNotFoundException(string msg) : base(msg) { }
    }
}
