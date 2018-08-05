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
            var editor = new TextEditor();

            editor.SaveAndClose += (object s, TextEditor.ScriptSaveEventArgs scriptData) =>
            {
                var scriptCode = scriptData.Code.Replace("\r", "");
                var scriptHash = ScriptManagerPlugin.GetMD5Hash(scriptCode);
                var scriptEntry = new ScriptEntry()
                {
                    Name = scriptData.Title,
                    MD5Hash = scriptHash,
                    Code = scriptCode,
                    Enabled = false
                };
                (DataContext as ScriptManagerConfig).Whitelist.Add(scriptEntry);
            };
        }
        public void OpenAddFromWorkshopDialog(object sender, RoutedEventArgs e)
        {
            Log.Info("Open AddFromWorkshop Dialog..");
        }

        private void DataGrid_TargetUpdated(object sender, DataTransferEventArgs e)
        {
            Plugin.Save();
        }

        private class TextEditor : Window
        {
            private TextBox TitleEditor;
            private TextBox CodeEditor;
            private Button SaveButton;
            public delegate void ScriptSaveEventHandler(object sender, ScriptSaveEventArgs e);
            public event ScriptSaveEventHandler SaveAndClose;

            public TextEditor( ) : base()
            {
                Width = 800;
                Height = 800;

                var titleLabel = new Label() { Content = "Script Name:" };
                TitleEditor = new TextBox()
                {
                    Width = 250
                };
                CodeEditor = new TextBox() {
                    TextWrapping = TextWrapping.NoWrap,
                    AcceptsReturn = true,
                    AcceptsTab = true,
                    IsReadOnly = false,
                    Width = 800,
                    Height = 600
                };
                SaveButton = new Button() { Content = "Save", HorizontalAlignment = HorizontalAlignment.Center };
                Thickness margin = SaveButton.Margin;
                margin.Top = 10;
                SaveButton.Margin = margin;
                SaveButton.Click += Save;

                var titleGrid = new Grid() { Width  = 250 };
                titleGrid.RowDefinitions.Add(new RowDefinition());
                titleGrid.ColumnDefinitions.Add(new ColumnDefinition());
                titleGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(10) });
                titleGrid.ColumnDefinitions.Add(new ColumnDefinition() );
                //titleGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = GridLength.Auto });

                titleGrid.Children.Add(titleLabel);
                titleGrid.Children.Add(TitleEditor);
                Grid.SetColumn(TitleEditor, 2);

                var stackPanel = new StackPanel();
                stackPanel.Children.Add(titleGrid);
                //stackPanel.Children.Add(TitleEditor);
                stackPanel.Children.Add(CodeEditor);
                stackPanel.Children.Add(SaveButton);
                AddChild(stackPanel);
                Show();
            }

            protected void OnSaveAndClose(ScriptSaveEventArgs e)
            {
                SaveAndClose?.Invoke(this, e);
            }

            private void Save(object sender, RoutedEventArgs e)
            {
                if (TitleEditor.Text == "")
                    return;
                OnSaveAndClose(new ScriptSaveEventArgs() { Title = TitleEditor.Text, Code = CodeEditor.Text });
                Close();
            }

            public class ScriptSaveEventArgs : EventArgs
            {
                public string Title;
                public string Code;
            }

        }
    }
}
