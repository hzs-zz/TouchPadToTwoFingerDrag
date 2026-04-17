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

            // 设置数据上下文，让 ListBox 能看到名单
            this.DataContext = wadpy_pn_dataSource;
        }

        // 🌟 添加按钮逻辑
        private void wadpy_pn_AddButton_Click(object sender, RoutedEventArgs e)
        {
            string newProcess = wadpy_pn_ProcessInput.Text.Trim();

            // 简单的格式处理：去掉可能带有的 .exe 后缀，统一存入
            if (newProcess.ToLower().EndsWith(".exe"))
            {
                newProcess = newProcess.Substring(0, newProcess.Length - 4);
            }

            if (!string.IsNullOrEmpty(newProcess))
            {
                if (!wadpy_pn_dataSource.Contains(newProcess))
                {
                    wadpy_pn_dataSource.Add(newProcess);
                    wadpy_pn_ProcessInput.Clear();
                    // 自动把焦点还给输入框，方便连续输入
                    wadpy_pn_ProcessInput.Focus();
                }
                else
                {
                    // 如果已存在，可以闪烁一下或者清空
                    wadpy_pn_ProcessInput.Clear();
                }
            }
        }

        // 支持回车键添加
        private void wadpy_pn_ProcessInput_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                wadpy_pn_AddButton_Click(sender, e);
                // 防止回车键触发其他系统音
                e.Handled = true;
            }
        }

        // 🌟 删除按钮逻辑
        private void wadpy_pn_DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            // 在 WPF 中，sender 是按钮，按钮的 DataContext 自动就是那一行的字符串
            var btn = sender as Wpf.Ui.Controls.Button;
            var nameToRemove = btn?.DataContext as string;

            if (nameToRemove != null)
            {
                wadpy_pn_dataSource.Remove(nameToRemove);
            }
        }

        private void GithubButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo("https://github.com/YourUsername") { UseShellExecute = true });
            }
            catch { /* 忽略浏览器启动异常 */ }
        }
    }
}