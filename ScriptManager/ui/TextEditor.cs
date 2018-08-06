using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

namespace ScriptManager
{
    public class TextEditor : Window
    {
        private enum MessageType { Warning, Error, Notification };
        private TextBox TitleEditor;
        private TextBox CodeEditor;
        private Button SaveButton;
        private Label MessageBox;
        public delegate void ScriptSaveEventHandler(object sender, ScriptSaveEventArgs e);
        public event ScriptSaveEventHandler SaveAndClose;

        public TextEditor() : base()
        {
            var marginSize = 12;

            Width = 800;
            MaxHeight = System.Windows.Forms.Screen.PrimaryScreen.Bounds.Height * 0.7;
            SizeToContent = SizeToContent.Height;

            var titleLabel = new Label() { Content = "Script Name:" };
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
                MinHeight = 400,
                MaxWidth = Width - (2 * marginSize)
            };
            Thickness margin = CodeEditor.Margin;
            margin.Top = margin.Bottom = margin.Left = margin.Right = marginSize;
            SaveButton = new Button() { Content = "Save", HorizontalAlignment = HorizontalAlignment.Center };
            margin = SaveButton.Margin;
            margin.Top = marginSize;
            SaveButton.Margin = margin;
            SaveButton.Click += Save;

            MessageBox = new Label()
            {
                Height = FontSize * 5,
                Background = Brushes.LightGray,
                BorderBrush = Brushes.DarkGray,
                BorderThickness = new Thickness(1),
                VerticalContentAlignment = VerticalAlignment.Center,
                HorizontalContentAlignment = HorizontalAlignment.Center
            };
            margin = MessageBox.Margin;
            margin.Top = margin.Left = margin.Right = margin.Bottom = marginSize;
            MessageBox.Margin = margin;

            var titleGrid = new Grid()
            {
                Width = Double.NaN,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Top,
                Height = Double.NaN
            };
            margin = titleGrid.Margin;
            margin.Bottom = marginSize;
            titleGrid.Margin = margin;

            titleGrid.RowDefinitions.Add(new RowDefinition());
            titleGrid.ColumnDefinitions.Add(new ColumnDefinition());
            titleGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(10) });
            titleGrid.ColumnDefinitions.Add(new ColumnDefinition());
            //titleGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = GridLength.Auto });

            titleGrid.Children.Add(titleLabel);
            titleGrid.Children.Add(TitleEditor);
            Grid.SetColumn(TitleEditor, 2);

            var stackPanel = new StackPanel()
            {
                Height = Double.NaN
            };
            stackPanel.Children.Add(titleGrid);
            //stackPanel.Children.Add(TitleEditor);
            stackPanel.Children.Add(CodeEditor);
            stackPanel.Children.Add(SaveButton);
            stackPanel.Children.Add(MessageBox);

            AddChild(stackPanel);
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
            if (TitleEditor.Text == "")
            {
                ShowMessage("Script must have a title!", MessageType.Error, 5000);
                return;
            }
            if(CodeEditor.Text == "")
            {
                ShowMessage("You need to insert a script!", MessageType.Error, 5000);
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
                    MessageBox.Foreground = Brushes.Red;
                    break;
                case MessageType.Warning:
                    MessageBox.Foreground = Brushes.Orange;
                    break;
                case MessageType.Notification:
                    MessageBox.Foreground = Brushes.Black;
                    break;
            }
            MessageBox.Content = message;
            //MessageBox.Visibility = Visibility.Visible;

            if (duration > 0)
                Task.Delay(duration).ContinueWith(_ =>
                {
                    MessageBox.Content = "";
                });
        }

    }
}
