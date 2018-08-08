using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using NLog;

namespace ScriptManager
{
    public class TextEditor : Window
    {
        private enum MessageType { Warning, Error, Notification };
        private TextBox TitleEditor;
        private Label TitleLabel;
        private TextBox CodeEditor;
        private Button SaveButton;
        private Label NotificationBox;
        private Grid TitleGrid;
        private Grid RootContainer;
        private double staticElementsHeight;
        public delegate void ScriptSaveEventHandler(object sender, ScriptSaveEventArgs e);
        public event ScriptSaveEventHandler SaveAndClose;
        private static readonly Logger Log = LogManager.GetLogger("ScriptManager");

        public TextEditor() : base()
        {
            var marginSize = 12;

            Width = 800;
            Height = System.Windows.Forms.Screen.PrimaryScreen.Bounds.Height * 0.7;
            
            //SizeToContent = SizeToContent.Height;

            TitleLabel = new Label() { Content = "Script Name:" };
            TitleEditor = new TextBox()
            {
                Width = 250,
                Height = FontSize * 1.5
            };

            CodeEditor = new TextBox()
            {
                TextWrapping = TextWrapping.NoWrap,
                AcceptsReturn = true,
                AcceptsTab = true,
                IsReadOnly = false,
                Height = Double.NaN,
                //MaxWidth = Width,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto
            };
            Thickness margin = CodeEditor.Margin;
            margin.Top = margin.Bottom = margin.Left = margin.Right = marginSize;
            CodeEditor.Margin = margin;
            SaveButton = new Button() { Content = "Save", HorizontalAlignment = HorizontalAlignment.Center };
            margin = SaveButton.Margin;
            margin.Top = marginSize;
            SaveButton.Margin = margin;
            SaveButton.Click += Save;

            NotificationBox = new Label()
            {
                Height = FontSize * 5,
                Background = Brushes.LightGray,
                BorderBrush = Brushes.DarkGray,
                BorderThickness = new Thickness(1),
                VerticalContentAlignment = VerticalAlignment.Center,
                HorizontalContentAlignment = HorizontalAlignment.Center
            };
            margin = NotificationBox.Margin;
            margin.Top = margin.Left = margin.Right = margin.Bottom = marginSize;
            NotificationBox.Margin = margin;

            TitleGrid = new Grid()
            {
                Width = Double.NaN,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Top,
                Height = Double.NaN
            };
            margin = TitleGrid.Margin;
            margin.Bottom = marginSize;
            TitleGrid.Margin = margin;

            TitleGrid.RowDefinitions.Add(new RowDefinition());
            TitleGrid.ColumnDefinitions.Add(new ColumnDefinition());
            TitleGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(10) });
            TitleGrid.ColumnDefinitions.Add(new ColumnDefinition());
            //titleGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = GridLength.Auto });

            TitleGrid.Children.Add(TitleLabel);
            TitleGrid.Children.Add(TitleEditor);
            Grid.SetColumn(TitleEditor, 2);
            staticElementsHeight = 0D;
            //Height = System.Windows.Forms.Screen.PrimaryScreen.Bounds.Height * 0.8;
            MinHeight = 400;

            RootContainer = new Grid()
            {
                VerticalAlignment = VerticalAlignment.Stretch
            };
            RootContainer.RowDefinitions.Add(new RowDefinition()
            {
                Height = GridLength.Auto
            });
            RootContainer.RowDefinitions.Add(new RowDefinition());
            RootContainer.RowDefinitions.Add(new RowDefinition()
            {
                Height = GridLength.Auto
            });
            RootContainer.RowDefinitions.Add(new RowDefinition()
            {
                Height = GridLength.Auto
            });
            RootContainer.Children.Add(TitleGrid);
            RootContainer.Children.Add(CodeEditor);
            RootContainer.Children.Add(SaveButton);
            RootContainer.Children.Add(NotificationBox);
            Grid.SetRow(TitleGrid, 0);
            Grid.SetRow(CodeEditor, 1);
            Grid.SetRow(SaveButton, 2);
            Grid.SetRow(NotificationBox, 3);

            AddChild(RootContainer);
        }

        private Double GetValueOrZero(double value)
        {
            return Double.IsNaN(value) ? 0 : value;
        }

        public void LoadScript(ScriptEntry script)
        {
            TitleEditor.Text = script.Name;
            CodeEditor.Text = script.Code;
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
            if(string.IsNullOrEmpty(CodeEditor.Text))
            {
                ShowMessage("You need to insert a script!", MessageType.Error, 5000);
                return;
            }
            OnSaveAndClose(new ScriptSaveEventArgs() { Title = TitleEditor.Text, Code = CodeEditor.Text });
            Close();
        }

        public class ScriptSaveEventArgs : EventArgs
        {
            public string Title;
            public string Code;
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

    }
}
