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
using Torch.Views;
using NLog;

namespace ScriptManager
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
            var editor = new CollectionEditor() { Owner = Window.GetWindow(this) };
            var script = new List<string>();
            editor.Edit<string>(script, "Whitelist");
            Log.Info("Got script!");
            var scriptText = String.Join("\n", script);
            Log.Info(scriptText);
            (DataContext as ScriptManagerConfig).Whitelist.Add(new ScriptEntry() { Name = "New Script", MD5Hash = ScriptManagerPlugin.GetMD5Hash(scriptText), Enabled = false });
        }
        public void OpenAddFromWorkshopDialog(object sender, RoutedEventArgs e)
        {
            Log.Info("Open AddFromWorkshop Dialog..");
        }
    }
}
