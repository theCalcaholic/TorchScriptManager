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
        //public string StatusMessage = "";
        //public bool DownloadInProgress = false;
        public AddFromWorkshopDialog()
        {
            InitializeComponent();
            //Log.Info("Creating new DownloadStatus object");
            Status = new DownloadStatus();
            DataContext = Status;
            Status.PropertyChanged += (object sender, PropertyChangedEventArgs e) => { UpdateLayout(); };
            AllowsTransparency = false;
        }

        public async void AddScript(object sender, RoutedEventArgs e)
        {
            Log.Info("Retrieving script details...");
            Status.IsInProgress = true;
            Status.StatusMessage = "Downloading Script Details...";
            var shouldClose = false;
            var delay = 3000;

            string statusMsg = "";

            if (!ulong.TryParse(WorkshopIDEditor.Text, out ulong workshopId))
            {
                statusMsg = "Invalid input! The workshop ID must be a positive number.";
                Log.Warn(statusMsg);
            }
            else
            {

                var workshopService = SteamWorkshopService.Instance;
                var scriptData = workshopService.GetPublishedFileDetails(new ulong[] { workshopId })?[workshopId];

                if (scriptData == null)
                {
                    statusMsg = $"Failed to retrieve script for workshop id '{workshopId}'!";
                    Log.Error(statusMsg);
                    statusMsg = "ERROR: " + statusMsg;
                }
                else
                {
                    statusMsg = $"Script successful retrieved!";
                    Log.Info(statusMsg);

                    if( scriptData.ConsumerAppId != Util.AppID )
                    {
                        statusMsg = $"Invalid AppID! The downloaded object is for app {scriptData.ConsumerAppId}, expected: {Util.AppID}.";
                        Log.Error(statusMsg);
                        statusMsg = "ERROR: " + statusMsg;
                    }
                    else if( !scriptData.Tags.Contains("ingameScript") )
                    {
                        statusMsg = $"Retrieved object is not an ingame script!";
                        Log.Error(statusMsg);
                        statusMsg = "ERROR: " + statusMsg;
                    }
                    else
                    {
                        shouldClose = true;
                        delay = 1000;
                        ScriptManagerPlugin.Instance.Config.Whitelist.Add(new ScriptEntry()
                        {
                            Name = scriptData.Title,
                            WorkshopID = scriptData.PublishedFileId,
                            Code = ""
                        });
                    }

                }
            }
            Status.StatusMessage += "\n\n" + statusMsg;

            await Task.Delay(delay);
            Status.IsInProgress = false;
            if(shouldClose)
                Close();
        }
    }

    public class DownloadStatus : INotifyPropertyChanged
    {
        private static readonly Logger Log = LogManager.GetLogger("ScriptManagerUI");
        public event PropertyChangedEventHandler PropertyChanged;

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
                    OnPropertyChanged(new PropertyChangedEventArgs(nameof(IsInProgress)));
                }
            }
        }

        private void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            Log.Info($"Notifying property change of {e.PropertyName}");
            PropertyChanged?.Invoke(this, e);
            if (PropertyChanged == null)
                Log.Warn("Property Changed handler is null!");
        }
    }
}
