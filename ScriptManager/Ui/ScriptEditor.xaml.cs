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
using System.ComponentModel;
using Torch;
using NLog;

namespace ScriptManager.Ui
{
    /// <summary>
    /// Interaction logic for TextEditor.xaml
    /// </summary>
    public partial class ScriptEditor : Window
    {
        private enum MessageType { Warning, Error, Notification };
        public delegate void ScriptSaveEventHandler(object sender, ScriptSaveEventArgs e);
        public event ScriptSaveEventHandler SaveAndClose;
        private static readonly Logger Log = LogManager.GetLogger("ScriptManager");
        private bool hasScriptBeenLoaded = false;

        public ScriptEditor()
        {
            InitializeComponent();
            DataContext = new ScriptData();
            (DataContext as ScriptData).PropertyChanged += (object sender, PropertyChangedEventArgs e) =>
            {
                if (!hasScriptBeenLoaded)
                    return;

                var scriptData = sender as ScriptData;
                if (e.PropertyName == nameof(ScriptData.KeepUpdated) && scriptData.KeepUpdated )
                {
                    var button = MessageBoxButton.YesNo;
                    var icon = MessageBoxImage.Warning;
                    var result = MessageBox.Show(
                        "Are you sure ?\nThis will overwrite any manual changes to the code on server reboot!", 
                        "Warning", button, icon);
                    if (result != MessageBoxResult.Yes)
                    {
                        hasScriptBeenLoaded = false;
                        scriptData.KeepUpdated = false;
                        hasScriptBeenLoaded = true;
                    }
                }
            };

        }

        public void HideWorkshopSettings()
        {
            (DataContext as ScriptData).WorkshopEditable = false;
        }

        public void ShowWorkshopSettings()
        {
            (DataContext as ScriptData).WorkshopEditable = true;
        }

        public void LoadScript(ScriptEntry script)
        {
            var context = DataContext as ScriptData;
            context.Name = script.Name;
            context.Code = script.Code;
            context.WorkshopID = script.WorkshopID;
            context.KeepUpdated = script.KeepUpdated;
            if (context.WorkshopID == 0)
                HideWorkshopSettings();
            hasScriptBeenLoaded = true;
        }

        protected void OnSaveAndClose(ScriptSaveEventArgs e)
        {
            SaveAndClose?.Invoke(this, e);
        }

        private void Save(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(TitleEditor.Text))
            {
                ShowMessage("Script must have a title!", MessageType.Error, 5000);
                return;
            }
            if (string.IsNullOrEmpty(CodeEditor.Text))
            {
                ShowMessage("You need to insert a script!", MessageType.Error, 5000);
                return;
            }
            OnSaveAndClose(new ScriptSaveEventArgs() { Script = DataContext as ScriptData });
            Close();
        }

        private void ShowMessage(string message, MessageType type, int duration = 0)
        {
            switch (type)
            {
                case MessageType.Error:
                    message = "ERROR: " + message;
                    NotificationBox.Foreground = Brushes.Red;
                    break;
                case MessageType.Warning:
                    NotificationBox.Foreground = Brushes.Orange;
                    break;
                case MessageType.Notification:
                    NotificationBox.Foreground = Brushes.Black;
                    break;
            }
            NotificationBox.Content = message;
            //MessageBox.Visibility = Visibility.Visible;

            if (duration > 0)
                Task.Delay(duration).ContinueWith(_ =>
                {
                    NotificationBox.Content = "";
                });
        }

        public class ScriptSaveEventArgs : EventArgs
        {
            public ScriptData Script;
        }
    }
    public class ScriptData : ViewModel
    {
        private bool _workshopEditable = false;
        public bool WorkshopEditable
        {
            get => _workshopEditable;
            set
            {
                SetValue(ref _workshopEditable, value);
            }
        }

        private string _name;
        public string Name
        {
            get => _name;
            set
            {
                SetValue(ref _name, value);
            }
        }
        private string _code;
        public string Code
        {
            get => _code;
            set
            {
                SetValue(ref _code, value);
            }
        }
        private ulong _workshopId;
        public ulong WorkshopID
        {
            get => _workshopId;
            set
            {
                SetValue(ref _workshopId, value);
            }
        }
        private bool _keepUpdated;
        public bool KeepUpdated
        {
            get => _keepUpdated;
            set
            {
                SetValue(ref _keepUpdated, value);
            }
        }
    }
}
