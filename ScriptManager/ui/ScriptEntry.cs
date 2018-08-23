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
using VRage;
using VRage.FileSystem;
using System.IO;
using Sandbox.ModAPI;
using SteamWorkshopTools;
using SteamWorkshopTools.Types;

namespace ScriptManager.Ui
{
    public class ScriptEntry : ViewModel
    {
        private static Logger Log = LogManager.GetLogger("ScriptManager");
        private bool _enabled;
        private static long nextId = 0;
        private static List<long> assignedIds = new List<long>();

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
                if( value == _id)
                {
                    if (!assignedIds.Contains(value))
                        assignedIds.Add(value);
                    return;
                }

                var code = Code;

                if (assignedIds.Contains(value))
                {
                    if (assignedIds.Contains(value))
                        Log.Warn($"A script with id {value} already exists! Current id ({_id}) will be kept.");
                    else
                    {
                        Log.Warn($"A script with id {value} already exists! Will retrieve new valid id isntead.");
                        _id = GetValidId();
                    }
                }
                else
                {
                    var pos = assignedIds.IndexOf(_id);
                    if(pos != -1)
                        assignedIds.RemoveAt(pos);
                    _id = value;
                    //Log.Info($"new nextId is {nextId}");
                }
                assignedIds.Add(_id);

                Code = code;
                MD5Hash = Util.GetMD5Hash(Code);
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
        [XmlIgnore]
        public string MD5Hash
        {
            get => _md5Hash;
            set
            {
                SetValue(ref _md5Hash, value);
            }
        }

        //[XmlIgnore]
        public string Code
        {
            get
            {
                if (ScriptManagerPlugin.Instance?.Config?.SaveLoadMode ?? true)
                    return "";

                var scriptPath = GetScriptPath();
                if( MyFileSystem.FileExists(scriptPath) )
                {
                    var code = File.ReadAllText(scriptPath);
                    if(code == null)
                    {
                        Log.Warn($"Error reading script '{Name}' (Id: {Id})!");
                        return "";
                    }
                    return code;
                }
                else
                {
                    Log.Warn($"Script code could not be found for script '{Name}' (Id: {Id})!");
                    return "";
                }
            }
            set
            {
                if (ScriptManagerPlugin.Instance?.Config?.SaveLoadMode ?? true)
                    return;

                if (value == null)
                {
                    Log.Warn("Received invalid value for script code!");
                    return;
                }

                var scriptPath = GetScriptPath();
                var code = value.Replace("\r\n", " \n");
                Directory.CreateDirectory(Path.GetDirectoryName(scriptPath));
                File.WriteAllText(scriptPath, value);
                MD5Hash = Util.GetMD5Hash(value);
                OnPropertyChanged();
                //UpdateRunning();
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
                //if (value)
                //    _needsUpdate = true;
                OnPropertyChanged();
            }
        }

        [XmlIgnore]
        public int InstallCount
        {
            get => ProgrammableBlocks.Count;
        }

        private ObservableCollection<long> _programmableBlocks = new ObservableCollection<long>();

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
            Id = GetValidId();

            var script = this;
            ProgrammableBlocks.CollectionChanged += (object sender, NotifyCollectionChangedEventArgs e) => {
                /*if (e.Action == NotifyCollectionChangedAction.Remove || e.Action == NotifyCollectionChangedAction.Replace)
                    foreach (long pbId in e.OldItems)
                        ScriptManagerPlugin.Instance.Config.RunningScripts.Remove(pbId);
                if (e.Action == NotifyCollectionChangedAction.Replace || e.Action == NotifyCollectionChangedAction.Add)
                    foreach (long pbId in e.OldItems)
                        ScriptManagerPlugin.Instance.Config.RunningScripts[pbId] = script;*/
                OnPropertyChanged("InstallCount");
            };
        }

        public async Task<bool> UpdateFromWorkshopAsync(Action<string> messageHandler = null)
        {
            if (WorkshopID == 0)
                return true;


            var msg = "";
            PublishedItemDetails scriptInfo = null;
            try
            {
                messageHandler?.Invoke("Fetching script details...");
                scriptInfo = await UpdateDetailsFromWorkshopAsync();
            }
            catch (Exception e)
            {
                msg = $"Script Information could not be retrieved from Workshop!\n{e.Message}";
                Log.Error(msg);
                messageHandler?.Invoke("ERROR: " + msg);
                return false;
            }
            if ( scriptInfo == null)
            {
                msg = "Script Information could not be retrieved from Workshop!";
                Log.Error(msg);
                messageHandler?.Invoke("ERROR: " + msg);
                return false;
            }

            if(IsScriptUpToDate(scriptInfo))
            {
                Code = ReadScriptFromArchive();
                msg = $"Script '{Name}' is up to date.";
                Log.Info(msg);
                messageHandler?.Invoke(msg);
                //_needsUpdate = false;
                return true;
            }

            msg = "Downloading script code...";
            Log.Info(msg);
            messageHandler?.Invoke(msg);
            try
            {
                await UpdateCodeFromWorkshopAsync(scriptInfo);
            }
            catch (Exception e)
            {
                msg = $"Code could not be downloaded from Workshop!\n{e.Message}";
                Log.Error(msg);
                messageHandler?.Invoke("ERROR: " + msg);
                return false;
            }
            //_needsUpdate = false;
            return true;
        }

        public async Task<PublishedItemDetails> UpdateDetailsFromWorkshopAsync()
        {
            string statusMsg;
            var workshopService = SteamWorkshopService.Instance;
            var scriptData = (await workshopService.GetPublishedFileDetails(new ulong[] { WorkshopID }))?[WorkshopID];

            if (scriptData == null)
            {
                throw new Exception($"Failed to retrieve script for workshop id '{WorkshopID}'!");
            }
            else
            {
                if (scriptData.ConsumerAppId != Util.AppID)
                {
                    throw new Exception($"Invalid AppID! The requested object is for app {scriptData.ConsumerAppId}, expected: {Util.AppID}.");
                }
                else if (!scriptData.Tags.Contains("ingameScript"))
                {
                    throw new Exception($"The requested object is not an ingame script!");
                }
                else
                {
                    Log.Info(statusMsg = $"Script info successfully retrieved!");
                    Name = scriptData.Title;
                    WorkshopID = scriptData.PublishedFileId;

                    return scriptData;
                }
            }
        }

        public async Task UpdateCodeFromWorkshopAsync(PublishedItemDetails fileDetails)
        {
            var workshopService = SteamWorkshopService.Instance;
            try
            {
                await workshopService.DownloadPublishedFile(fileDetails, ScriptManagerPlugin.ScriptsPath, WorkshopID + ".sbs");
            }
            catch (Exception e)
            {
                throw new Exception("An error occured while downloading script code: \n" + e.Message);
            }

            string code = null;
            var readTask = Task.Run(delegate
            {
                code = ReadScriptFromArchive();
            });
            if (await Task.WhenAny(readTask, Task.Delay(10000)) == readTask)
            {
                Code = code;
            }
            else
            {
                throw new Exception($"An error occured while reading downloaded script '{Name}'!");
            }
        }

        public void Delete()
        {
            var scriptPath = GetScriptPath();
            if (File.Exists(scriptPath))
                File.Delete(scriptPath);
            var scriptWSPath = Path.Combine(ScriptManagerPlugin.ScriptsPath, WorkshopID + ".sbs");
            if (File.Exists(scriptWSPath))
                File.Delete(scriptWSPath);
        }

        public void UpdateRunning()
        {
            long pbId;
            var code = Code;
            List<int> removeList = new List<int>();
            for (int i = 0; i < ProgrammableBlocks.Count; i++ )
            {
                pbId = ProgrammableBlocks[i];
                if (MyAPIGateway.Entities.GetEntityById(pbId) is IMyProgrammableBlock pb)
                    pb.ProgramData = code;
                else
                {
                    Log.Warn($"Programmable block running script '{Name}' not found, removing from list...");
                    removeList.Add(i);
                }
            }
            foreach (var index in removeList)
                ProgrammableBlocks.RemoveAtFast(index);

        }

        private string GetScriptPath()
        {
            return Path.Combine(ScriptManagerPlugin.ScriptsPath, $"ingame_script_{Id.ToString().PadLeft(4, '0')}.cs");
        }

        private string ReadScriptFromArchive()
        {
            var scriptPath = Path.Combine(ScriptManagerPlugin.ScriptsPath, WorkshopID + ".sbs");
            string text = null;

            foreach (string file in MyFileSystem.GetFiles(scriptPath, ".cs", MySearchOption.AllDirectories))
            {
                if (MyFileSystem.FileExists(file))
                {
                    using (Stream stream = MyFileSystem.OpenRead(file))
                    {
                        using (StreamReader streamReader = new StreamReader(stream))
                        {
                            text = streamReader.ReadToEnd();
                        }
                    }
                }
            }
            return text;
        }

        private bool IsScriptUpToDate(PublishedItemDetails script)
        {
            string scriptPath = Path.Combine(ScriptManagerPlugin.ScriptsPath, WorkshopID + ".sbs");
            if (script.FileSize > 0L)
            {
                if (!File.Exists(scriptPath))
                    return false;

                using (FileStream fileStream = File.OpenRead(scriptPath))
                {
                    if (fileStream.Length != script.FileSize)
                        return false;
                }

            }

            return File.GetLastWriteTimeUtc(scriptPath) >= script.TimeUpdated;
        }

        private long GetValidId()
        {
            long id;
            for (id = 0; id <= long.MaxValue; id++)
            {
                if (!assignedIds.Contains(id))
                    return id;
            }
            throw new Exception("Can't assign id: Maximum number of scripts reached!");
        }
    }


    public class ScriptNotFoundException : Exception
    {
        public ScriptNotFoundException(string msg) : base(msg) { }
    }
}
