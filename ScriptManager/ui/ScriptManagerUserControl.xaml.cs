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
using System.Windows.Markup;
using Torch.Views;
using NLog;
using ScriptManager;

namespace ScriptManager.Ui
{
    /// <summary>
    /// Interaction logic for ScriptmanagerUserControl.xaml
    /// </summary>
    public partial class ScriptManagerUserControl : UserControl
    {
        public ScriptManagerPlugin Plugin;
        private static readonly Logger Log = LogManager.GetLogger("ScriptManagerUI");

        public ScriptManagerUserControl()
        {
            InitializeComponent();

        }

        public void OpenAddFromCodeDialog(object sender, RoutedEventArgs e)
        {
            var editor = new ScriptEditor();

            editor.SaveAndClose += (object s, ScriptEditor.ScriptSaveEventArgs scriptData) =>
            {
                var scriptCode = scriptData.Script.Code.Replace("\r", "");
                //var scriptHash = ScriptManagerPlugin.GetMD5Hash(scriptCode);
                var scriptEntry = new ScriptEntry()
                {
                    Name = scriptData.Script.Name,
                    //MD5Hash = scriptHash,
                    Code = scriptCode,
                    Enabled = false
                };
                (DataContext as ScriptManagerConfig).Whitelist.Add(scriptEntry);
            };

            editor.Show();
        }
        public void OpenAddFromWorkshopDialog(object sender, RoutedEventArgs e)
        {
            var dialog = new AddFromWorkshopDialog();
            dialog.Show();
        }

        private void WhitelistUpdated(object sender, DataTransferEventArgs e)
        {
            Plugin.Save();
        }

        private void EditSelectedScript(object sender, RoutedEventArgs e)
        {
            var editor = new ScriptEditor();
            var script = WhitelistTable.SelectedItem as ScriptEntry;
            editor.LoadScript(script);

            editor.SaveAndClose += (object s, ScriptEditor.ScriptSaveEventArgs scriptData) =>
            {
                var scriptCode = scriptData.Script.Code.Replace("\r", "");
                //var scriptHash = ScriptManagerPlugin.GetMD5Hash(scriptCode);
                script.Name = scriptData.Script.Name;
                script.Code = scriptCode;
                //script.MD5Hash = scriptHash;
                WhitelistUpdated(this, null);
            };

            editor.Show();

        }

        private void RemoveSelectedScript(object sender, RoutedEventArgs e)
        {
            var scriptEntry = (WhitelistTable.SelectedItem as ScriptEntry);
            scriptEntry.Delete();
            (DataContext as ScriptManagerConfig).Whitelist.Remove(scriptEntry);
        }
    }
}
