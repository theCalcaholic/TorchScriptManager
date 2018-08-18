using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using ScriptManager;
using System.ComponentModel;
using Sandbox.Engine.Networking;
using NLog;

using Torch;
using SteamWorkshopTools;

namespace ScriptManager.Ui
{
    /// <summary>
    /// Interaction logic for AddFromWorkshopDialog.xaml
    /// </summary>
    public partial class AddFromWorkshopDialog : Window
    {
        private static readonly Logger Log = LogManager.GetLogger("ScriptManagerUI");
        public ScriptData Result;
        public DownloadStatus Status;
        public AddFromWorkshopDialog()
        {
            InitializeComponent();
            Log.Info("Creating new DownloadStatus object");
            //Status = new DownloadStatus();
            //DataContext = Status;
        }

        public void AddScript(object sender, RoutedEventArgs e)
        {
            if( !ulong.TryParse(WorkshopIDEditor.Text, out ulong workshopId) )
            {
                Log.Warn("Invalid input! The workshop ID must be a positive number.");
                return;
            }

            var workshopService = SteamWorkshopService.Instance;
            /*Task logonTask;
            if (!workshopService.IsReady)
            {
                logonTask = workshopService.Logon();
                var start = DateTime.Now;
                while ( !logonTask.IsCompleted && !logonTask.IsFaulted && !logonTask.IsCanceled )
                {
                    logonTask.Wait(TimeSpan.FromSeconds(1));
                    if (DateTime.Now - start > TimeSpan.FromSeconds(30))
                        workshopService.CancelLogon();
                }
                if( logonTask.IsCanceled )
                {
                    Log.Error("Logging into steam timed out!");
                    return;
                }
                else if( logonTask.IsFaulted )
                {
                    Log.Error($"An error occured while logging into steam: {logonTask.Exception.Message}");
                    return;
                }
            }*/

            Log.Info("Retrieving script details...");
            var scriptData = workshopService.GetPublishedFileDetails(new ulong[] { workshopId })?[workshopId];

            if (scriptData == null)
            {
                Log.Warn($"Failed to retrieve script for workshop id '{workshopId}'!");
                return;
            }

            ScriptManagerPlugin.Instance.Config.Whitelist.Add(new ScriptEntry()
            {
                Name = scriptData.Title,
                WorkshopID = scriptData.PublishedFileId,
                Code = ""
            });
            Close();
            return;

            var task = WorkshopTools.GetScriptInfoAsync(workshopId);
            var status = DataContext as DownloadStatus;
            status.HasStarted = true;
            status.StatusMessage = "Downloading Script...";

            task.Wait(TimeSpan.FromSeconds(30));
            status.IsComplete = task.IsCompleted;
            status.HasFailed = task.IsCanceled || task.IsFaulted;
            if (task.IsFaulted)
            {
                status.StatusMessage = "An Error occured while downloading! \n\n" + task.Exception.Message;
            }
            else if (task.IsCanceled)
            {
                status.StatusMessage = "An Error occured while downloading: Timeout after 30 seconds.";
            }
            else
            {
                var script = task.Result as MyWorkshop.SubscribedItem;
                ScriptManagerPlugin.Instance.Config.Whitelist.Add(new ScriptEntry()
                {
                    Name = script.Title,
                    WorkshopID = script.PublishedFileId,
                    Code = "",
                });
            }
        }
    }

    public class DownloadStatus : INotifyPropertyChanged
    {
        private string _statusMessage = "";
        public string StatusMessage
        {
            get => _statusMessage;
            set
            {
                if( _statusMessage != value )
                {
                    _statusMessage = value;
                    OnPropertyChanged(new PropertyChangedEventArgs(nameof(StatusMessage)));
                }
                
            }
        }
        private bool _isInProgress = false;
        public bool IsInProgress
        {
            get => _isInProgress;
            set
            {
                if( _isInProgress != value )
                {
                    _isInProgress = value;
                    HasStarted = true;
                    IsComplete = false;
                    HasFailed = false;
                    OnPropertyChanged(new PropertyChangedEventArgs(nameof(IsInProgress)));
                }
            }
        }
        private bool _isComplete = false;
        public bool IsComplete
        {
            get => _isComplete;
            set
            {
                if (_isComplete != value)
                {
                    _isComplete = value;
                    IsInProgress = false;
                    OnPropertyChanged(new PropertyChangedEventArgs(nameof(IsComplete)));
                }

            }
        }
        private bool _hasStarted = false;
        public bool HasStarted
        {
            get => _hasStarted;
            set
            {
                if (_hasStarted != value)
                {
                    _hasStarted = value;
                    IsInProgress = true;
                    OnPropertyChanged(new PropertyChangedEventArgs(nameof(HasStarted)));
                }

            }
        }
        private bool _hasFailed = false;
        public bool HasFailed
        {
            get => _hasFailed;
            set
            {
                if (_hasFailed != value)
                {
                    _hasFailed = value;
                    IsInProgress = false;
                    OnPropertyChanged(new PropertyChangedEventArgs(nameof(HasFailed)));
                }

            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            PropertyChanged?.Invoke(this, e);
        }
    }
}
