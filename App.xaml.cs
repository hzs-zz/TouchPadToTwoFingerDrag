using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Threading;
using TouchpadToMiddleClick;
using Timer = System.Timers.Timer;

namespace TouchpadToMiddleClick
{
    public partial class App : Application
    {
        private TwoFingerLogic wadpy_pn_logicBrain = new TwoFingerLogic();
        private SettingsWindow? wadpy_pn_SettingsWin;
        private ForegroundTracker? wadpy_pn_Radar;

        // 🌟 全局共享的动态进程名单
        private ObservableCollection<string> wadpy_pn_GlobalProcessList = new ObservableCollection<string>
        {
            "SLDWORKS", "acad", "Rhino"
        };

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // 应用浅色主题以获得“白底黑字”的 Win11 质感
            Wpf.Ui.Appearance.ApplicationThemeManager.Apply(Wpf.Ui.Appearance.ApplicationTheme.Light);
            this.ShutdownMode = ShutdownMode.OnExplicitShutdown;

            // 1. 优先弹出界面
            ShowSettingsWindow();

            // 2. 🌟 异步延迟加载硬件驱动，防止启动时消息循环死锁
            Dispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle, new Action(() =>
            {
                if (TouchpadHelper.Exists())
                {
                    TouchpadHelper.RegisterInput();
                    MouseHookManager.Start();

                    // 初始化雷达并传入共享名单
                    wadpy_pn_Radar = new ForegroundTracker(wadpy_pn_GlobalProcessList);
                    wadpy_pn_Radar.OnFocusChanged = Wadpy_pn_HandleFocusChanged;
                    wadpy_pn_Radar.Start();

                    TouchpadHelper.OnContactsReceived = (contacts) =>
                    {
                        MouseHookManager.IsTouchpadActive = contacts.Count > 0;
                        if (MouseHookManager.IsDriverActive) wadpy_pn_logicBrain.Process(contacts);
                    };

                    TouchpadHelper.OnToggleHotkey = () => {
                        bool isEnabled = !MouseHookManager.IsDriverActive;
                        MouseHookManager.IsDriverActive = isEnabled;
                        if (!isEnabled) wadpy_pn_logicBrain.Process(new List<TouchpadContact>());
                        Dispatcher.Invoke(() => StatusToast.Show(isEnabled ? "🚀 驱动已开启" : "⏸️ 驱动已挂起"));
                    };

                    TouchpadHelper.OnSettingsHotkey = () => Dispatcher.Invoke(() => ShowSettingsWindow());
                }
            }));
        }

        private void ShowSettingsWindow()
        {
            if (wadpy_pn_SettingsWin == null)
            {
                wadpy_pn_SettingsWin = new SettingsWindow(wadpy_pn_GlobalProcessList);

                // 修复 NullReferenceException 的关键：提前指定 MainWindow
                if (Application.Current.MainWindow == null)
                    Application.Current.MainWindow = wadpy_pn_SettingsWin;

                wadpy_pn_SettingsWin.Closed += (s, args) => {
                    if (Application.Current.MainWindow == wadpy_pn_SettingsWin)
                        Application.Current.MainWindow = null;
                    wadpy_pn_SettingsWin = null;
                };
                wadpy_pn_SettingsWin.Show();
            }
            else
            {
                if (wadpy_pn_SettingsWin.WindowState == WindowState.Minimized)
                    wadpy_pn_SettingsWin.WindowState = WindowState.Normal;
                wadpy_pn_SettingsWin.Activate();
            }
        }

        private void Wadpy_pn_HandleFocusChanged(string processName, bool isTarget)
        {
            Dispatcher.Invoke(() =>
            {
                if (MouseHookManager.IsDriverActive != isTarget)
                {
                    MouseHookManager.IsDriverActive = isTarget;
                    if (!isTarget) wadpy_pn_logicBrain.Process(new List<TouchpadContact>());
                    StatusToast.Show(isTarget ? $"🚀 自动接管: {processName}" : "⏸️ 驱动挂起 (失去焦点)");
                }
            });
        }

        protected override void OnExit(ExitEventArgs e)
        {
            wadpy_pn_Radar?.Stop();
            MouseHookManager.Stop();
            base.OnExit(e);
        }
    }
}