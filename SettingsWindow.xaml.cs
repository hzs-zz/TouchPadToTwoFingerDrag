using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Wpf.Ui.Controls;

namespace TouchpadToMiddleClick
{
    public class ProcessConfig
    {
        public string ProcessName { get; set; } = "";

        // 🌟 反向规则（黑名单）开关
        public bool IsReverseRule { get; set; } = false;

        public ObservableCollection<string> TargetClasses { get; set; } = new ObservableCollection<string>();
    }

    public class ConfigContainer
    {
        public bool IsPanEnabled { get; set; } = true;
        public bool IsScrollEnabled { get; set; } = true;
        public bool ShowToastNotifications { get; set; } = true;

        public ObservableCollection<ProcessConfig> PanProcesses { get; set; } = new ObservableCollection<ProcessConfig>();
        public ObservableCollection<ProcessConfig> ScrollProcesses { get; set; } = new ObservableCollection<ProcessConfig>();
    }

    public partial class SettingsWindow : FluentWindow
    {
        private ConfigContainer _config;

        public SettingsWindow(ConfigContainer config)
        {
            InitializeComponent();
            _config = config;
            this.DataContext = _config;

            UpdateSwitches();
        }

        public void UpdateSwitches()
        {
            PanSwitch.Checked -= PanSwitch_Checked;
            PanSwitch.Unchecked -= PanSwitch_Unchecked;
            ScrollSwitch.Checked -= ScrollSwitch_Checked;
            ScrollSwitch.Unchecked -= ScrollSwitch_Unchecked;
            ToastSwitch.Checked -= ToastSwitch_Checked;
            ToastSwitch.Unchecked -= ToastSwitch_Unchecked;

            PanSwitch.IsChecked = _config.IsPanEnabled;
            ScrollSwitch.IsChecked = _config.IsScrollEnabled;
            ToastSwitch.IsChecked = _config.ShowToastNotifications;

            PanSwitch.Checked += PanSwitch_Checked;
            PanSwitch.Unchecked += PanSwitch_Unchecked;
            ScrollSwitch.Checked += ScrollSwitch_Checked;
            ScrollSwitch.Unchecked += ScrollSwitch_Unchecked;
            ToastSwitch.Checked += ToastSwitch_Checked;
            ToastSwitch.Unchecked += ToastSwitch_Unchecked;
        }

        // --- 🌟 恢复：三个主开关卡片的点击控制 ---
        private void PanCard_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) { PanSwitch.IsChecked = !PanSwitch.IsChecked; e.Handled = true; }
        private void ScrollCard_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) { ScrollSwitch.IsChecked = !ScrollSwitch.IsChecked; e.Handled = true; }
        private void ToastCard_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) { ToastSwitch.IsChecked = !ToastSwitch.IsChecked; e.Handled = true; }

        // --- 🌟 反向规则卡片区域的整块点击控制 ---
        private void ReverseRuleCard_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement card)
            {
                var sw = FindVisualChild<Wpf.Ui.Controls.ToggleSwitch>(card);
                if (sw != null) sw.IsChecked = !sw.IsChecked;
            }
            e.Handled = true;
        }

        // 辅助函数：在卡片里找到那个 ToggleSwitch
        private T? FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
        {
            for (int i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = System.Windows.Media.VisualTreeHelper.GetChild(parent, i);
                if (child != null && child is T typedChild) return typedChild;
                else
                {
                    T? childOfChild = FindVisualChild<T>(child!);
                    if (childOfChild != null) return childOfChild;
                }
            }
            return null;
        }

        private void ReverseSwitch_Changed(object sender, RoutedEventArgs e)
        {
            (Application.Current as App)?.SaveConfig();
            (Application.Current as App)?.UpdateHookMasters();
        }

        // --- 全局设置逻辑 ---
        private void ToastSwitch_Checked(object sender, RoutedEventArgs e) { _config.ShowToastNotifications = true; (Application.Current as App)?.SaveConfig(); }
        private void ToastSwitch_Unchecked(object sender, RoutedEventArgs e) { _config.ShowToastNotifications = false; (Application.Current as App)?.SaveConfig(); }

        // --- 中键平移逻辑 ---
        private void PanSwitch_Checked(object sender, RoutedEventArgs e) { _config.IsPanEnabled = true; (Application.Current as App)?.UpdateHookMasters(); }
        private void PanSwitch_Unchecked(object sender, RoutedEventArgs e) { _config.IsPanEnabled = false; (Application.Current as App)?.UpdateHookMasters(); }

        private void AddPanProcess_Click(object sender, RoutedEventArgs e)
        {
            string name = NewPanProcessInput.Text.Trim();
            if (name.ToLower().EndsWith(".exe")) name = name.Substring(0, name.Length - 4);
            if (!string.IsNullOrEmpty(name))
            {
                _config.PanProcesses.Add(new ProcessConfig { ProcessName = name });
                NewPanProcessInput.Clear();
                (Application.Current as App)?.SaveConfig();
            }
        }
        private void RemovePanProcess_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as FrameworkElement)?.DataContext is ProcessConfig p) { _config.PanProcesses.Remove(p); (Application.Current as App)?.SaveConfig(); }
        }
        private void AddPanClass_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Wpf.Ui.Controls.Button;
            var input = btn?.Tag as Wpf.Ui.Controls.TextBox;
            if (btn?.DataContext is ProcessConfig p && !string.IsNullOrWhiteSpace(input?.Text))
            {
                if (!p.TargetClasses.Contains(input.Text.Trim())) p.TargetClasses.Add(input.Text.Trim());
                input.Clear();
                (Application.Current as App)?.SaveConfig();
            }
        }
        private void RemovePanClass_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Wpf.Ui.Controls.Button;
            if (btn?.Tag is ProcessConfig p && btn.DataContext is string c) { p.TargetClasses.Remove(c); (Application.Current as App)?.SaveConfig(); }
        }

        // --- 滚动模拟逻辑 ---
        private void ScrollSwitch_Checked(object sender, RoutedEventArgs e) { _config.IsScrollEnabled = true; (Application.Current as App)?.UpdateHookMasters(); }
        private void ScrollSwitch_Unchecked(object sender, RoutedEventArgs e) { _config.IsScrollEnabled = false; (Application.Current as App)?.UpdateHookMasters(); }

        private void AddScrollProcess_Click(object sender, RoutedEventArgs e)
        {
            string name = NewScrollProcessInput.Text.Trim();
            if (name.ToLower().EndsWith(".exe")) name = name.Substring(0, name.Length - 4);
            if (!string.IsNullOrEmpty(name))
            {
                _config.ScrollProcesses.Add(new ProcessConfig { ProcessName = name });
                NewScrollProcessInput.Clear();
                (Application.Current as App)?.SaveConfig();
            }
        }
        private void RemoveScrollProcess_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as FrameworkElement)?.DataContext is ProcessConfig p) { _config.ScrollProcesses.Remove(p); (Application.Current as App)?.SaveConfig(); }
        }
        private void AddScrollClass_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Wpf.Ui.Controls.Button;
            var input = btn?.Tag as Wpf.Ui.Controls.TextBox;
            if (btn?.DataContext is ProcessConfig p && !string.IsNullOrWhiteSpace(input?.Text))
            {
                if (!p.TargetClasses.Contains(input.Text.Trim())) p.TargetClasses.Add(input.Text.Trim());
                input.Clear();
                (Application.Current as App)?.SaveConfig();
            }
        }
        private void RemoveScrollClass_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Wpf.Ui.Controls.Button;
            if (btn?.Tag is ProcessConfig p && btn.DataContext is string c) { p.TargetClasses.Remove(c); (Application.Current as App)?.SaveConfig(); }
        }

        private void GithubButton_Click(object sender, RoutedEventArgs e)
        {
            try { Process.Start(new ProcessStartInfo("https://github.com/hzs-zz") { UseShellExecute = true }); } catch { }
        }
    }
}