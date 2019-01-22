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
using Torch.Server.Views;
using SteamWorkshopService;

namespace ScriptManager.Ui
{
    /// <summary>
    /// Interaction logic for AddFromWorkshopDialog.xaml
    /// </summary>
    public partial class AddFromWorkshopDialog : Window
    {
        private static readonly Logger Log = LogManager.GetLogger("ScriptManager");
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


            ThemeControl.UpdateDynamicControls += new Action<ResourceDictionary>(UpdateResourceDict);
            UpdateResourceDict(ThemeControl.currentTheme);
        }
        public void UpdateResourceDict(ResourceDictionary dictionary)
        {
            Resources.MergedDictionaries.Clear();
            Resources.MergedDictionaries.Add(dictionary);
        }

        public async void AddScript(object sender, RoutedEventArgs e)
        {
            Status.StatusMessage = "";
            Status.IsInProgress = true;
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
                var script = new ScriptEntry
                {
                   WorkshopID = workshopId
                };
                if( !await script.UpdateFromWorkshopAsync(msg => { Status.StatusMessage += "\n>" + msg; }) )
                {
                    statusMsg = "Failed to add script from workshop id";
                    Log.Error(statusMsg);
                }
                else
                {
                    shouldClose = true;
                    delay = 1000;
                    statusMsg = "Successfully added Script from Workshop.";
                    Log.Info(statusMsg);
                    ScriptManagerPlugin.Instance.Config.Whitelist.Add(script);
                }

            }
            Status.StatusMessage += "\n>" + statusMsg;

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
            PropertyChanged?.Invoke(this, e);
        }
    }
}
