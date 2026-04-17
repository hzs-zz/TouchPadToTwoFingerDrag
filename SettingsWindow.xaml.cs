using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Wpf.Ui.Controls;

namespace TouchpadToMiddleClick
{
    public partial class SettingsWindow : FluentWindow
    {
        private ObservableCollection<string> wadpy_pn_dataSource;

        public SettingsWindow(ObservableCollection<string> processes)
        {
            InitializeComponent();
            wadpy_pn_dataSource = processes;
            // 接通数据总线：将名单绑定到界面的 ListBox
            this.DataContext = wadpy_pn_dataSource;
        }

        private void wadpy_pn_AddButton_Click(object sender, RoutedEventArgs e)
        {
            string newProcess = wadpy_pn_ProcessInput.Text.Trim();
            if (!string.IsNullOrEmpty(newProcess) && !wadpy_pn_dataSource.Contains(newProcess))
            {
                wadpy_pn_dataSource.Add(newProcess);
                wadpy_pn_ProcessInput.Clear();
            }
        }

        private void wadpy_pn_ProcessInput_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter) wadpy_pn_AddButton_Click(sender, e);
        }

        private void wadpy_pn_DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            // 通过 DataContext 找到点击行对应的数据字符串
            var btn = sender as Wpf.Ui.Controls.Button;
            var name = btn?.DataContext as string;
            if (name != null) wadpy_pn_dataSource.Remove(name);
        }

        private void GithubButton_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(new ProcessStartInfo("https://github.com/YourUsername") { UseShellExecute = true });
        }
    }
}